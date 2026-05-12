namespace ST.Infra.Redis.Config;

public class RedisOptions
{
	public RedisOptions()
	{

	}

	public RedisOptions(string connectionString)
	{
		ConnectionString = connectionString;
	}
	///// <summary>
	///// 模式 Standalone,MasterReplica,Sentinel,Cluster
	///// </summary>
	//public RedisConnectionType ConnectionType { get; set; } = RedisConnectionType.Standalone;

	/// <summary>
	/// 连接字符串 
	/// </summary>
	public string? ConnectionString { get; set; }

	/// <summary>
	/// Gets the list of endpoints to be used to connect to the Redis server.
	/// </summary>
	/// <value>
	/// The endpoints.
	/// </value>
	public IList<string> Endpoints { get; } = [];

	/// <summary>
	/// 密码
	/// </summary>
	public string? Password { get; set; }

	/// <summary>默认数据库（集群模式下强制为0）</summary>
	public int? DefaultDatabase { get; set; }

	/// <summary>哨兵模式：主服务名称（仅哨兵模式需要）</summary>
	public string? ServiceName { get; set; }

	/// <summary>连接超时时间（毫秒）</summary>
	public int ConnectTimeout { get; set; } = 5000;

	/// <summary>
	/// 客户端名称，会显示在 Redis 的 CLIENT LIST 命令结果中，用于识别连接来源（如区分不同服务的连接）
	/// </summary>
	public string? ClientName { get; set; }

	/// <summary>同步超时时间（毫秒）</summary>
	public int SyncTimeout { get; set; } = 1000;

	/// <summary>
	/// 首次连接失败时是否终止连接
	/// </summary>
	public bool AbortOnConnectFail { get; set; } = false;

	/// <summary>
	/// 是否允许执行 Redis 管理员命令
	/// <para>
	/// 安全起见，默认应为 <c>false</c>。如果你需要在生产环境调用 Admin 命令，请显式开启。
	/// </para>
	/// </summary>
	public bool AllowAdmin { get; set; } = false;

	/// <summary>
	/// SSL 握手时验证的主机名
	/// </summary>
	public string? SslHost { get; set; }

	/// <summary>
	/// 是否启用 SSL 加密连接
	/// </summary>
	public bool Ssl { get; set; }
}
