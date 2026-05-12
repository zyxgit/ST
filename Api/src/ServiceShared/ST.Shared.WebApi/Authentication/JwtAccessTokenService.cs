using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ST.Shared.Const;
using ST.Shared.Security;

namespace ST.Shared.WebApi.Authentication;

public sealed class JwtAccessTokenService : IAccessTokenService
{
	private readonly JwtOptions _options;
	private readonly JwtSecurityTokenHandler _handler = new();

	public JwtAccessTokenService(IOptions<JwtOptions> options)
	{
		_options = options.Value;
	}

	public AccessTokenResult CreateToken(AccessTokenRequest request)
	{
		if (string.IsNullOrWhiteSpace(_options.SigningKey))
		{
			throw new InvalidOperationException($"JWT 签名密钥未配置，请设置 '{SettingPrefixContants.Jwt_SigningKey}'。");
		}

		var now = DateTimeOffset.UtcNow;
		var accessTokenLifetime = _options.AccessTokenSeconds.HasValue && _options.AccessTokenSeconds.Value > 0
			? TimeSpan.FromSeconds(_options.AccessTokenSeconds.Value)
			: TimeSpan.FromMinutes(_options.AccessTokenMinutes > 0 ? _options.AccessTokenMinutes : 60);
		var expiresAt = now.Add(accessTokenLifetime);

		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, request.UserId.ToString("D")),
			new(JwtRegisteredClaimNames.Email, request.Email),
			new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
		};

		if (!string.IsNullOrWhiteSpace(request.NickName))
		{
			claims.Add(new Claim(JwtClaimConstants.NickName, request.NickName));
		}

		foreach (var role in request.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			claims.Add(new Claim(JwtClaimConstants.Role, role));
		}

		foreach (var perm in request.Permissions.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			claims.Add(new Claim(JwtClaimConstants.Permission, perm));
		}

		var keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);
		var signingCredentials = new SigningCredentials(
			new SymmetricSecurityKey(keyBytes),
			SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: _options.Issuer,
			audience: _options.Audience,
			claims: claims,
			notBefore: now.UtcDateTime,
			expires: expiresAt.UtcDateTime,
			signingCredentials: signingCredentials);

		return new AccessTokenResult(_handler.WriteToken(token), expiresAt);
	}
}
