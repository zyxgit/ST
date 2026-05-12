using ST.MS.Identity.Domain.Aggregates.RoleAggregate;
using ST.MS.Identity.Domain.Aggregates.UserAggregate.ValueObject;
using ST.Shared.Exceptions;
using ST.Shared.Validation;

namespace ST.MS.Identity.Domain.Aggregates.UserAggregate;

/// <summary>
/// 用户信息
/// </summary>
public class User : AggregateRoot, ISoftDelete
{
	public User() { }

	public User(string email, Password password)
	{
		if (string.IsNullOrWhiteSpace(email))
			throw new BusinessException("邮箱不能为空");
		Id = Guid.CreateVersion7();
		Email = NormalizeAndValidateEmail(email);
		Password = password;
		NickName = $"用户{new string([.. email.Take(4)])}";
	}

	public User(string nickName, string phone, string email, Password password)
	{
		if (string.IsNullOrWhiteSpace(nickName))
			throw new BusinessException("昵称不能为空");

		if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
			throw new BusinessException("手机号或邮箱至少填写一个");

		NickName = nickName;
		Phone = NormalizeAndValidatePhone(phone);
		Email = NormalizeAndValidateEmail(email);
		Password = password;
		Id = Guid.CreateVersion7();
	}

	/// <summary>
	/// 昵称
	/// </summary>

	public string NickName { get; private set; } = string.Empty;

	/// <summary>
	/// 手机号
	/// </summary>

	public string Phone { get; private set; } = string.Empty;

	/// <summary>
	/// 邮箱
	/// </summary>

	public string Email { get; private set; } = string.Empty;

	/// <summary>
	/// 密码
	/// </summary>
	public Password Password { get; private set; } = null!;

	/// <summary>
	/// 激活状态
	/// </summary>
	public bool IsEnable { get; private set; } = true;

	/// <summary>
	/// 是否已删除
	/// </summary>
	public bool IsDeleted { get; set; }

	/// <summary>
	/// 最后登录时间
	/// </summary>
	public DateTime? LastLoginTime { get; set; }

	/// <summary>
	/// 最后登录IP
	/// </summary>
	public string? LastLoginIp { get; set; }

	/// <summary>
	/// 头像文件ID（来自 FileUpload 服务）
	/// </summary>
	public Guid? AvatarFileId { get; private set; }

	public List<UserRole> UserRoles { get; set; } = [];

	public List<Role> Role { get; set; } = [];


	#region 行为

	private static string NormalizeAndValidateEmail(string email)
	{
		email = email.Trim().ToLowerInvariant();

		if (!CommonRegex.Email().IsMatch(email))
			throw new DomainException("邮箱格式不正确");

		return email;
	}

	private static string NormalizeAndValidatePhone(string phone)
	{
		phone = phone.Trim();

		if (!CommonRegex.ChinaMobile().IsMatch(phone))
			throw new DomainException("手机号格式不正确");

		return phone;
	}

	private static string NormalizePhoneOptional(string? phone)
	{
		return string.IsNullOrWhiteSpace(phone) ? string.Empty : NormalizeAndValidatePhone(phone);
	}

	public void Disable()
	{
		IsEnable = false;
	}

	public void Enable()
	{
		IsEnable = true;
	}

	public void UpdateBasicInfo(string nickName, string email, string? phone)
	{
		if (string.IsNullOrWhiteSpace(nickName))
			throw new BusinessException("昵称不能为空");

		NickName = nickName.Trim();
		Email = NormalizeAndValidateEmail(email);
		Phone = NormalizePhoneOptional(phone);
	}

	public void ChangePassword(Password password)
	{
		Password = password ?? throw new BusinessException("密码不能为空");
	}

	public void SoftDelete()
	{
		IsDeleted = true;
	}

	public void RecordLogin(string loginIp)
	{
		LastLoginTime = DateTime.UtcNow;
		LastLoginIp = loginIp;
	}

	public void AssignRole(Guid roleId)
	{
		if (roleId == Guid.Empty)
			return;
		if (UserRoles.Any(x => x.RoleId == roleId))
			return;
		UserRoles.Add(new UserRole
		{
			UserId = Id,
			RoleId = roleId
		});
	}

	public void RemoveRole(Guid roleId)
	{
		UserRoles.RemoveAll(x => x.RoleId == roleId);
	}

	public void SetRoles(IEnumerable<Guid>? roleIds)
	{
		var targetRoleIds = roleIds?
			.Where(x => x != Guid.Empty)
			.Distinct()
			.ToHashSet() ?? [];

		UserRoles.RemoveAll(x => !targetRoleIds.Contains(x.RoleId));

		foreach (var roleId in targetRoleIds)
		{
			AssignRole(roleId);
		}
	}

	public void SetAvatar(Guid fileId)
	{
		AvatarFileId = fileId;
	}

	public void RemoveAvatar()
	{
		AvatarFileId = null;
	}


	#endregion



}
