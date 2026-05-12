using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.Infra.Core.Helper;
using ST.Infra.Email.Abstractions;
using ST.Infra.EventBus.Abstractions;
using ST.Infra.EventBus.Events;
using ST.Infra.Redis.Cache;
using ST.Infra.Tasks.Abstractions;
using ST.MS.Identity.Application.Dtos.User;
using ST.MS.Identity.Application.IServices;
using ST.MS.Identity.Application.Options;
using ST.MS.Identity.Domain.Aggregates.UserAggregate;
using ST.MS.Identity.Domain.Aggregates.UserAggregate.ValueObject;
using ST.MS.Identity.Domain.Enums;
using ST.MS.Identity.Domain.Services;
using ST.MS.Identity.Infra.DbContext;
using ST.Shared.Application.Dtos;
using ST.Shared.Application.Services;
using ST.Shared.Security;
using ST.Shared.Validation;
using StackExchange.Redis;

namespace ST.MS.Identity.Application.Services;

public class UserService : AbstractAppService, IUserService
{
	private readonly CodeManager _codeManager;
	private readonly IRedisCacheManager _redisCacheManager;
	private readonly IEmailSender _emailSender;
	private readonly IdentityDbContext _dbContext;
	private readonly IBackgroundTaskScheduler _taskScheduler;
	private readonly IDatabase _redis;
	private readonly IAccessTokenService _accessTokenService;
	private readonly IUserContext _userContext;
	private readonly IRefreshTokenLifetimeProvider _refreshTokenLifetimeProvider;
	private readonly IEventBus _eventBus;
	private readonly ILogger<UserService> _logger;
	private readonly IdentitySessionOptions _sessionOptions;

	public UserService(
		CodeManager codeManager,
		IRedisCacheManager redisCacheManager,
		IEmailSender emailSender,
		IdentityDbContext dbContext,
		IBackgroundTaskScheduler taskScheduler,
		IAccessTokenService accessTokenService,
		IUserContext userContext,
		IRefreshTokenLifetimeProvider refreshTokenLifetimeProvider,
		IOptions<IdentitySessionOptions> sessionOptions,
		IEventBus eventBus,
		ILogger<UserService> logger)
	{
		_codeManager = codeManager;
		_redisCacheManager = redisCacheManager;
		_emailSender = emailSender;
		_dbContext = dbContext;
		_taskScheduler = taskScheduler;
		_accessTokenService = accessTokenService;
		_userContext = userContext;
		_refreshTokenLifetimeProvider = refreshTokenLifetimeProvider;
		_sessionOptions = sessionOptions.Value;
		_eventBus = eventBus;
		_logger = logger;
		_redis = redisCacheManager.GetDatabase();
	}

	/// <summary>
	/// 注册
	/// </summary>
	public async Task RegisterAsync(RegisterInputDto input)
	{
		if (string.IsNullOrWhiteSpace(input.Email))
			throw new BusinessException("邮箱不能为空");
		if (!CommonRegex.Email().IsMatch(input.Email))
			throw new BusinessException("邮箱格式错误");

		if (string.IsNullOrWhiteSpace(input.Password))
			throw new BusinessException("密码不能为空");

		var verifyKey = GetVerifyCodeKey(input.Email, CodePurpose.Register);
		var dbCode = await _redis.StringGetAsync(verifyKey);
		_codeManager.Verify(dbCode, input.EmailVerifyCode);
		await _redis.KeyDeleteAsync(verifyKey);

		var exists = await _dbContext.Users.AnyAsync(x => x.Email == input.Email);
		if (exists)
			throw new BusinessException("邮箱已被注册");

		var user = new User(input.Email, BuildPassword(input.Password));

		_dbContext.Users.Add(user);
		await _dbContext.SaveChangesAsync();
	}

	/// <summary>
	/// 发送邮件验证码
	/// </summary>
	public async Task SendEmailCodeAsync(SendEmailInputDto input)
	{
		if (string.IsNullOrWhiteSpace(input.Email))
			throw new BusinessException("邮箱不能为空");
		if (!CommonRegex.Email().IsMatch(input.Email))
			throw new BusinessException("邮箱格式错误");

		await CheckSendLimitAsync(input.Email, input.CodePurpose);

		var code = _codeManager.GenerateCode();
		await _redis.StringSetAsync(GetVerifyCodeKey(input.Email, input.CodePurpose), code, TimeSpan.FromMinutes(5));

		_taskScheduler.Enqueue(async ct =>
		{
			await _emailSender.SendAsync(input.Email, "验证码", $"您的验证码是：{code}，5 分钟内有效", ct);
		});
	}

	/// <summary>
	/// 登录（返回 AccessToken + RefreshToken）
	/// </summary>
	public async Task<LoginResultDto> LoginAsync(UserLoginInputDto input)
	{
		if (input.Email.IsNullOrEmpty() || !CommonRegex.Email().IsMatch(input.Email))
			throw new BusinessException("邮箱格式不正确");
		if (input.Password.IsNullOrEmpty())
			throw new BusinessException("密码不能为空");

		var user = await _dbContext.Users
			.Include(u => u.UserRoles)
			.ThenInclude(ur => ur.Role)
			.ThenInclude(r => r.RolePermissions)
			.ThenInclude(rp => rp.Permission)
			.FirstOrDefaultAsync(x => x.Email == input.Email)
			?? throw new BusinessException("未查询到邮箱注册信息");

		if (!user.IsEnable)
			throw new ArgumentException("账号已锁定");

		if (!PasswordHelper.VerifyPassword(input.Password, user.Password.Hash, user.Password.Salt))
		{
			var failCount = await SetLoginFailCountAsync(user.Id);

			if (failCount > 5)
			{
				user.Disable();
				await _dbContext.SaveChangesAsync();
				await RemoveLoginFailCountAsync(user.Id);
				throw new BusinessException("账号已锁定");
			}

			throw new BusinessException("密码错误");
		}

		await RemoveLoginFailCountAsync(user.Id);

		var (accessToken, expiresAt) = CreateAccessToken(user);
		var (refreshToken, refreshExpiresAt, refreshTokenEntity) = CreateRefreshTokenEntity(user.Id);

		_dbContext.RefreshTokens.Add(refreshTokenEntity);

		var loginIp = GetClientIpOrUnknown();
		user.RecordLogin(loginIp);

		await _dbContext.SaveChangesAsync();
		await EnforceRefreshSessionLimitAsync(user.Id, "session-limit");

		try
		{
			await _eventBus.PublishAsync(new UserLoginSucceededIntegrationEvent(
				user.Id,
				user.Email,
				loginIp,
				DateTimeOffset.UtcNow));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Publish UserLoginSucceededIntegrationEvent failed. UserId={UserId}", user.Id);
		}

		return new LoginResultDto
		{
			AccessToken = accessToken,
			ExpiresAt = expiresAt,
			RefreshToken = refreshToken,
			RefreshTokenExpiresAt = refreshExpiresAt
		};
	}

	/// <summary>
	/// 刷新 Token（旋转 RefreshToken）
	/// </summary>
	public async Task<LoginResultDto> RefreshTokenAsync(RefreshTokenInputDto input)
	{
		if (input.RefreshToken.IsNullOrEmpty())
			throw new BusinessException("RefreshToken 不能为空");

		var tokenHash = ComputeSha256Base64(input.RefreshToken);

		var refresh = await _dbContext.RefreshTokens
			.FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

		if (refresh == null)
			throw new BusinessException("RefreshToken 无效");

		if (refresh.IsRevoked)
			throw new BusinessException("RefreshToken 已失效");

		if (refresh.ExpiresAtUtc <= DateTime.UtcNow)
			throw new BusinessException("RefreshToken 已过期");

		var user = await _dbContext.Users
			.Include(u => u.UserRoles)
			.ThenInclude(ur => ur.Role)
			.ThenInclude(r => r.RolePermissions)
			.ThenInclude(rp => rp.Permission)
			.FirstOrDefaultAsync(u => u.Id == refresh.UserId)
			?? throw new BusinessException("用户不存在");

		if (!user.IsEnable)
			throw new BusinessException("账号已锁定");

		var nowIp = GetClientIpOrUnknown();

		var (newRefreshToken, newRefreshExpiresAt, newRefreshEntity) = CreateRefreshTokenEntity(user.Id);
		refresh.Revoke(DateTime.UtcNow, nowIp, newRefreshEntity.TokenHash);
		_dbContext.RefreshTokens.Add(newRefreshEntity);

		var (accessToken, expiresAt) = CreateAccessToken(user);

		await _dbContext.SaveChangesAsync();
		await EnforceRefreshSessionLimitAsync(user.Id, "session-limit");

		return new LoginResultDto
		{
			AccessToken = accessToken,
			ExpiresAt = expiresAt,
			RefreshToken = newRefreshToken,
			RefreshTokenExpiresAt = newRefreshExpiresAt
		};
	}

	/// <summary>
	/// 注销（撤销 RefreshToken）
	/// </summary>
	public async Task LogoutAsync(RefreshTokenInputDto input)
	{
		if (input.RefreshToken.IsNullOrEmpty())
			return;

		var tokenHash = ComputeSha256Base64(input.RefreshToken);
		var refresh = await _dbContext.RefreshTokens
			.FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

		if (refresh == null || refresh.IsRevoked)
			return;

		refresh.Revoke(DateTime.UtcNow, GetClientIpOrUnknown(), null);
		await _dbContext.SaveChangesAsync();
	}

	public async Task<IReadOnlyList<UserOptionDto>> GetUsersAsync()
	{
		return await _dbContext.Users
			.AsNoTracking()
			.OrderBy(x => x.NickName)
			.Select(x => new UserOptionDto
			{
				Id = x.Id,
				NickName = x.NickName
			})
			.ToListAsync();
	}

	public async Task<IReadOnlyList<RoleOptionDto>> GetRoleOptionsAsync()
	{
		return await _dbContext.Role
			.AsNoTracking()
			.OrderBy(x => x.Name)
			.Select(x => new RoleOptionDto
			{
				Id = x.Id,
				Code = x.Code,
				Name = x.Name
			})
			.ToListAsync();
	}

	public async Task<PagedResultDto<UserListItemDto>> GetUserPageAsync(UserQueryInputDto input)
	{
		var (pageIndex, pageSize, skip) = input.Normalize();

		var query = _dbContext.Users
			.AsNoTracking()
			.Include(x => x.UserRoles)
			.ThenInclude(x => x.Role)
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(input.Keyword))
		{
			var keyword = input.Keyword.Trim();
			query = query.Where(x =>
				x.NickName.Contains(keyword) ||
				x.Email.Contains(keyword) ||
				x.Phone.Contains(keyword));
		}

		if (input.IsEnable.HasValue)
		{
			query = query.Where(x => x.IsEnable == input.IsEnable.Value);
		}

		if (input.RoleId.HasValue)
		{
			query = query.Where(x => x.UserRoles.Any(ur => ur.RoleId == input.RoleId.Value));
		}

		var totalCount = await query.LongCountAsync();
		var users = await query
			.OrderByDescending(x => x.CreateTime)
			.Skip(skip)
			.Take(pageSize)
			.ToListAsync();

		return new PagedResultDto<UserListItemDto>
		{
			PageIndex = pageIndex,
			PageSize = pageSize,
			TotalCount = totalCount,
			Items = users.Select(MapUserListItem).ToList()
		};
	}

	public async Task<UserDetailDto> GetUserDetailAsync(Guid id)
	{
		var user = await LoadUserForManageAsync(id);
		return MapUserDetail(user);
	}

	public async Task<Guid> CreateUserAsync(CreateUserInputDto input)
	{
		await ValidateUserInputAsync(input.Email, input.Phone, null);
		var roleIds = await ValidateRoleIdsAsync(input.RoleIds);

		var user = new User(input.NickName, input.Phone ?? string.Empty, input.Email, BuildPassword(input.Password));
		if (!input.IsEnable)
		{
			user.Disable();
		}

		user.SetRoles(roleIds);

		_dbContext.Users.Add(user);
		await _dbContext.SaveChangesAsync();
		return user.Id;
	}

	public async Task UpdateUserAsync(Guid id, UpdateUserInputDto input)
	{
		var user = await LoadUserForManageAsync(id);
		await ValidateUserInputAsync(input.Email, input.Phone, id);
		var roleIds = await ValidateRoleIdsAsync(input.RoleIds);

		user.UpdateBasicInfo(input.NickName, input.Email, input.Phone);
		user.SetRoles(roleIds);

		if (input.IsEnable)
		{
			user.Enable();
		}
		else
		{
			user.Disable();
			await RevokeRefreshTokensAsync(user.Id, "admin-disable");
		}

		await _dbContext.SaveChangesAsync();
	}

	public async Task<bool> EmailExistsAsync(string email, Guid? excludeUserId)
	{
		if (string.IsNullOrWhiteSpace(email))
			throw new BusinessException("邮箱不能为空");
		if (!CommonRegex.Email().IsMatch(email))
			throw new BusinessException("邮箱格式错误");

		var normalizedEmail = email.Trim().ToLowerInvariant();
		return await _dbContext.Users.AnyAsync(x => x.Email == normalizedEmail && x.Id != excludeUserId);
	}

	public async Task ChangeEmailAsync(ChangeEmailInputDto input)
	{
		if (!_userContext.IsAuthenticated || !_userContext.UserId.HasValue)
			throw new BusinessException("当前登录状态无效");

		var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == _userContext.UserId.Value)
			?? throw new BusinessException("用户不存在");

		var currentEmail = user.Email.Trim().ToLowerInvariant();
		var newEmail = input.NewEmail.Trim().ToLowerInvariant();

		if (currentEmail == newEmail)
			throw new BusinessException("新邮箱不能与当前邮箱相同");

		if (await EmailExistsAsync(newEmail, user.Id))
			throw new BusinessException("邮箱已存在");

		var currentVerifyKey = GetVerifyCodeKey(currentEmail, CodePurpose.ChangeEmail);
		var currentCode = await _redis.StringGetAsync(currentVerifyKey);
		_codeManager.Verify(currentCode, input.CurrentEmailVerifyCode);

		var newVerifyKey = GetVerifyCodeKey(newEmail, CodePurpose.ChangeEmail);
		var newCode = await _redis.StringGetAsync(newVerifyKey);
		_codeManager.Verify(newCode, input.NewEmailVerifyCode);

		await _redis.KeyDeleteAsync(currentVerifyKey);
		await _redis.KeyDeleteAsync(newVerifyKey);

		user.UpdateBasicInfo(user.NickName, newEmail, user.Phone);
		await _dbContext.SaveChangesAsync();
	}

	public async Task ChangeUserStatusAsync(Guid id, ChangeUserStatusInputDto input)
	{
		var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id)
			?? throw new BusinessException("用户不存在");

		if (_userContext.UserId == id && !input.IsEnable)
			throw new BusinessException("不能禁用当前登录用户");

		if (input.IsEnable)
		{
			user.Enable();
		}
		else
		{
			user.Disable();
			await RevokeRefreshTokensAsync(user.Id, "admin-disable");
		}

		await _dbContext.SaveChangesAsync();
	}

	public async Task ResetPasswordAsync(Guid id, ResetUserPasswordInputDto input)
	{
		var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id)
			?? throw new BusinessException("用户不存在");

		user.ChangePassword(BuildPassword(input.Password));
		await RevokeRefreshTokensAsync(user.Id, "password-reset");
		await _dbContext.SaveChangesAsync();
	}

	public async Task DeleteUserAsync(Guid id)
	{
		var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id)
			?? throw new BusinessException("用户不存在");

		if (_userContext.UserId == id)
			throw new BusinessException("不能删除当前登录用户");

		user.Disable();
		user.SoftDelete();
		await RevokeRefreshTokensAsync(user.Id, "soft-delete");
		await _dbContext.SaveChangesAsync();
	}

	public async Task SetUserAvatarAsync(Guid id, SetUserAvatarInputDto input)
	{
		var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id)
			?? throw new BusinessException("用户不存在");

		if (!input.AvatarFileId.HasValue)
			throw new BusinessException("头像文件ID不能为空");

		user.SetAvatar(input.AvatarFileId.Value);
		await _dbContext.SaveChangesAsync();
	}

	public async Task DeleteUserAvatarAsync(Guid id)
	{
		var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id)
			?? throw new BusinessException("用户不存在");

		user.RemoveAvatar();
		await _dbContext.SaveChangesAsync();
	}

	private (string AccessToken, DateTimeOffset ExpiresAt) CreateAccessToken(User user)
	{
		var roles = user.UserRoles
			.Where(r => r.Role != null)
			.Select(r => r.Role!.Code)
			.Where(c => !c.IsNullOrEmpty())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		var permissions = user.UserRoles
			.Where(x => x.Role != null)
			.SelectMany(x => x.Role!.RolePermissions)
			.Where(x => x.Permission != null && !x.Permission.Code.IsNullOrEmpty())
			.Select(x => x.Permission.Code)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		var token = _accessTokenService.CreateToken(new AccessTokenRequest(
			user.Id,
			user.Email,
			user.NickName,
			roles,
			permissions));

		return (token.AccessToken, token.ExpiresAt);
	}

	private (string RefreshToken, DateTimeOffset RefreshExpiresAt, RefreshToken Entity) CreateRefreshTokenEntity(Guid userId)
	{
		var rawToken = CreateSecureToken();
		var tokenHash = ComputeSha256Base64(rawToken);

		var lifetime = _refreshTokenLifetimeProvider.GetLifetime();
		var expiresAt = DateTimeOffset.UtcNow.Add(lifetime);

		var entity = new RefreshToken
		{
			Id = Guid.CreateVersion7(),
			UserId = userId,
			TokenHash = tokenHash,
			CreatedAtUtc = DateTime.UtcNow,
			ExpiresAtUtc = expiresAt.UtcDateTime,
			CreatedByIp = GetClientIpOrUnknown(),
			ReplacedByTokenHash = null
		};

		return (rawToken, expiresAt, entity);
	}

	private async Task<User> LoadUserForManageAsync(Guid id)
	{
		return await _dbContext.Users
			.Include(x => x.UserRoles)
			.ThenInclude(x => x.Role)
			.FirstOrDefaultAsync(x => x.Id == id)
			?? throw new BusinessException("用户不存在");
	}

	private async Task ValidateUserInputAsync(string email, string? phone, Guid? excludeUserId)
	{
		if (string.IsNullOrWhiteSpace(email))
			throw new BusinessException("邮箱不能为空");
		if (!CommonRegex.Email().IsMatch(email))
			throw new BusinessException("邮箱格式错误");

		var normalizedEmail = email.Trim().ToLowerInvariant();
		var emailExists = await _dbContext.Users.AnyAsync(x => x.Email == normalizedEmail && x.Id != excludeUserId);
		if (emailExists)
			throw new BusinessException("邮箱已存在");

		if (string.IsNullOrWhiteSpace(phone))
			return;

		if (!CommonRegex.ChinaMobile().IsMatch(phone))
			throw new BusinessException("手机号格式错误");

		var normalizedPhone = phone.Trim();
		var phoneExists = await _dbContext.Users.AnyAsync(x => x.Phone == normalizedPhone && x.Id != excludeUserId);
		if (phoneExists)
			throw new BusinessException("手机号已存在");
	}

	private async Task<IReadOnlyCollection<Guid>> ValidateRoleIdsAsync(IEnumerable<Guid>? roleIds)
	{
		var ids = roleIds?
			.Where(x => x != Guid.Empty)
			.Distinct()
			.ToArray() ?? [];

		if (ids.Length == 0)
			return ids;

		var existingIds = await _dbContext.Role
			.AsNoTracking()
			.Where(x => ids.Contains(x.Id))
			.Select(x => x.Id)
			.ToListAsync();

		if (existingIds.Count != ids.Length)
			throw new BusinessException("存在无效的角色Id");

		return existingIds;
	}

	private Password BuildPassword(string rawPassword)
	{
		if (string.IsNullOrWhiteSpace(rawPassword))
			throw new BusinessException("密码不能为空");

		var salt = PasswordHelper.GenerateSaltBase64();
		return new Password(PasswordHelper.HashPassword(rawPassword, salt), salt);
	}

	private async Task RevokeRefreshTokensAsync(Guid userId, string reason)
	{
		var now = DateTime.UtcNow;
		var revokedByIp = $"{GetClientIpOrUnknown()}:{reason}";
		var tokens = await _dbContext.RefreshTokens
			.Where(x => x.UserId == userId && x.RevokedAtUtc == null)
			.ToListAsync();

		foreach (var token in tokens)
		{
			token.Revoke(now, revokedByIp, null);
		}
	}

	private async Task EnforceRefreshSessionLimitAsync(Guid userId, string reason)
	{
		var now = DateTime.UtcNow;
		var revokedByIp = $"{GetClientIpOrUnknown()}:{reason}";
		var overflowTokens = await _dbContext.RefreshTokens
			.Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
			.OrderByDescending(x => x.CreatedAtUtc)
			.Skip(GetMaxActiveRefreshSessionsPerUser())
			.ToListAsync();

		if (overflowTokens.Count == 0)
			return;

		foreach (var token in overflowTokens)
		{
			token.Revoke(now, revokedByIp, null);
		}

		await _dbContext.SaveChangesAsync();
	}

	private int GetMaxActiveRefreshSessionsPerUser()
	{
		return _sessionOptions.MaxActiveRefreshSessionsPerUser > 0
			? _sessionOptions.MaxActiveRefreshSessionsPerUser
			: 5;
	}

	private string GetClientIpOrUnknown()
	{
		var ip = _userContext.ClientIp;
		return ip.IsNullOrEmpty() ? "unknown" : ip!;
	}

	private static UserListItemDto MapUserListItem(User user)
	{
		return new UserListItemDto
		{
			Id = user.Id,
			NickName = user.NickName,
			Email = user.Email,
			Phone = user.Phone,
			IsEnable = user.IsEnable,
			CreateTime = user.CreateTime,
			ModifyTime = user.ModifyTime,
			LastLoginTime = user.LastLoginTime,
			LastLoginIp = user.LastLoginIp,
			AvatarFileId = user.AvatarFileId,
			Roles = user.UserRoles
				.Where(x => x.Role != null)
				.Select(x => x.Role!.Name)
				.Distinct()
				.ToList()
		};
	}

	private static UserDetailDto MapUserDetail(User user)
	{
		return new UserDetailDto
		{
			Id = user.Id,
			NickName = user.NickName,
			Email = user.Email,
			Phone = user.Phone,
			IsEnable = user.IsEnable,
			CreateTime = user.CreateTime,
			ModifyTime = user.ModifyTime,
			LastLoginTime = user.LastLoginTime,
			LastLoginIp = user.LastLoginIp,
			AvatarFileId = user.AvatarFileId,
			Roles = user.UserRoles
				.Where(x => x.Role != null)
				.Select(x => new RoleOptionDto
				{
					Id = x.RoleId,
					Code = x.Role!.Code,
					Name = x.Role.Name
				})
				.OrderBy(x => x.Name)
				.ToList()
		};
	}

	private static string CreateSecureToken()
	{
		Span<byte> bytes = stackalloc byte[64];
		RandomNumberGenerator.Fill(bytes);
		return Base64UrlEncode(bytes);
	}

	private static string ComputeSha256Base64(string input)
	{
		var bytes = Encoding.UTF8.GetBytes(input);
		var hashBytes = SHA256.HashData(bytes);
		return Convert.ToBase64String(hashBytes);
	}

	private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
	{
		return Convert.ToBase64String(bytes)
			.TrimEnd('=')
			.Replace('+', '-')
			.Replace('/', '_');
	}

	private async Task<int> SetLoginFailCountAsync(Guid id)
	{
		var count = await _redis.StringIncrementAsync(GetLoginFailCountKey(id));
		return (int)count;
	}

	private async Task RemoveLoginFailCountAsync(Guid id)
	{
		await _redis.KeyDeleteAsync(GetLoginFailCountKey(id));
	}

	private async Task CheckSendLimitAsync(string target, CodePurpose purpose)
	{
		var cooldownKey = GetCooldownKey(target, purpose);
		var dailyKey = GetDailyKey(target, purpose);

		if (await _redis.KeyExistsAsync(cooldownKey))
			throw new BusinessException("请求过于频繁，请稍后再试");

		var count = await _redis.StringIncrementAsync(dailyKey);
		if (count == 1)
		{
			var expire = DateTime.Today.AddDays(1) - DateTime.Now;
			await _redis.KeyExpireAsync(dailyKey, expire);
		}

		if (count > 10)
			throw new BusinessException("今日验证码发送次数已达上限");

		await _redis.StringSetAsync(
			cooldownKey,
			"1",
			TimeSpan.FromSeconds(60));
	}

	private static string GetVerifyCodeKey(string target, CodePurpose purpose)
		=> $"verify:code:{purpose}:{target}";

	private static string GetCooldownKey(string target, CodePurpose purpose)
		=> $"verify:cooldown:{purpose}:{target}";

	private static string GetDailyKey(string target, CodePurpose purpose)
		=> $"verify:daily:{purpose}:{target}:{DateTime.Today:yyyyMMdd}";

	private static string GetLoginFailCountKey(Guid id)
		=> $"verify:login:fail:{id}";
}
