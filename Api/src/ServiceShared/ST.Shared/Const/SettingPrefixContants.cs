namespace ST.Shared.Const;

public class SettingPrefixContants
{
	public const string App = "App";

	public const string App_ErrorMessage = $"{App}:ErrorMessage";

	public const string App_CodeFirst = $"{App}:IsCodeFirst";

	public const string App_CodeFirst_IsCreateDatabase = $"{App}:IsCreateDatabase";

	public const string App_DataSeed = $"{App}:IsDataSeed";

	// 新约定：数据库配置
	public const string Database = "Database";
	public const string Database_Provider = $"{Database}:Provider";
	public const string Database_ConnectionString = $"{Database}:ConnectionString";

	// 新约定：JWT 配置
	public const string Jwt = "Jwt";
	public const string Jwt_Issuer = $"{Jwt}:Issuer";
	public const string Jwt_Audience = $"{Jwt}:Audience";
	public const string Jwt_SigningKey = $"{Jwt}:SigningKey";
	public const string Jwt_AccessTokenSeconds = $"{Jwt}:AccessTokenSeconds";
	public const string Jwt_AccessTokenMinutes = $"{Jwt}:AccessTokenMinutes";
	public const string Jwt_RefreshTokenSeconds = $"{Jwt}:RefreshTokenSeconds";
	public const string Jwt_RefreshTokenDays = $"{Jwt}:RefreshTokenDays";

	// 环境变量（配置系统会将 `__` 映射为 `:`）
	public const string Database_ConnectionString_Env = "Database__ConnectionString";

	// 旧约定（逐步迁移用）
	public const string DbConnectionString = "DbConnectionString";
	public const string DbConnectionString_DbType = $"{DbConnectionString}:DbType";
}
