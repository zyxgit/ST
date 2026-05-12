using Microsoft.Extensions.Configuration;
using ST.Shared.Const;
using ST.Shared.Security;

namespace ST.Shared.WebApi.Authentication;

public sealed class ConfigurationRefreshTokenLifetimeProvider : IRefreshTokenLifetimeProvider
{
	private readonly IConfiguration _configuration;

	public ConfigurationRefreshTokenLifetimeProvider(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public TimeSpan GetLifetime()
	{
		var seconds = _configuration.GetValue<int?>(SettingPrefixContants.Jwt_RefreshTokenSeconds);
		if (seconds.HasValue && seconds.Value > 0)
		{
			return TimeSpan.FromSeconds(seconds.Value);
		}

		// 默认 14 天
		var days = _configuration.GetValue<int?>(SettingPrefixContants.Jwt_RefreshTokenDays) ?? 14;
		if (days <= 0)
		{
			days = 14;
		}

		return TimeSpan.FromDays(days);
	}
}
