using ST.Infra.Redis.Config;

namespace ST.Infra.Redis.Provider;

public class RedisClientFactory : IRedisClient, IDisposable
{
	private readonly Lazy<ConnectionMultiplexer> _lazyConnectionMultiplexer;

	private readonly RedisOptions _redisOptions;

	public IConnectionMultiplexer GetConnection() => _lazyConnectionMultiplexer.Value;

	public IDatabase GetDatabase() => GetConnection().GetDatabase();

	public RedisClientFactory(RedisOptions redisOptions)
	{
		_redisOptions = redisOptions;
		_lazyConnectionMultiplexer = new Lazy<ConnectionMultiplexer>(CreateConnectionMultiplexer(redisOptions));
	}

	private static ConnectionMultiplexer CreateConnectionMultiplexer(RedisOptions redisOptions)
	{
		if (string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
		{
			if (redisOptions.Endpoints == null || redisOptions.Endpoints.Count == 0)
			{
				throw new InvalidOperationException("Redis 配置缺少 ConnectionString，且 Endpoints 为空。请在 Redis:Endpoints 中至少配置一个 endpoint，或提供 Redis:ConnectionString。");
			}

			var configurationOptions = new ConfigurationOptions
			{
				ConnectTimeout = redisOptions.ConnectTimeout,
				Password = redisOptions.Password,
				DefaultDatabase = redisOptions.DefaultDatabase,
				ServiceName = redisOptions.ServiceName,
				ClientName = redisOptions.ClientName,
				SyncTimeout = redisOptions.SyncTimeout,
				AbortOnConnectFail = redisOptions.AbortOnConnectFail,
				AllowAdmin = redisOptions.AllowAdmin,
				Ssl = redisOptions.Ssl,
				SslHost = redisOptions.SslHost,
				//TieBreaker = "",
				//CommandMap = CommandMap.Sentinel,
			};

			foreach (var endpoint in redisOptions.Endpoints)
			{
				configurationOptions.EndPoints.Add(endpoint);
			}

			return ConnectionMultiplexer.Connect(configurationOptions);
		}
		else
		{
			// 以 ConnectionString 为基础，再叠加 RedisOptions 中显式配置（避免“只有 ConnectionString 生效”的困惑）。
			var configurationOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString);

			// 仅当这些字段有值时再覆盖，避免把 connection string 里已有的设置覆盖掉。
			// 密码：如果外部配置了 Password，则覆盖 connection string。
			if (!string.IsNullOrWhiteSpace(redisOptions.Password))
			{
				configurationOptions.Password = redisOptions.Password;
			}

			if (redisOptions.DefaultDatabase.HasValue)
			{
				configurationOptions.DefaultDatabase = redisOptions.DefaultDatabase;
			}

			if (!string.IsNullOrWhiteSpace(redisOptions.ServiceName))
			{
				configurationOptions.ServiceName = redisOptions.ServiceName;
			}

			if (!string.IsNullOrWhiteSpace(redisOptions.ClientName))
			{
				configurationOptions.ClientName = redisOptions.ClientName;
			}

			// 超时/行为类字段没有空值语义，因此直接覆盖（如果你希望严格跟随 connection string，可后续再进一步把这些字段改为可空类型）。
			if (redisOptions.ConnectTimeout > 0)
			{
				configurationOptions.ConnectTimeout = redisOptions.ConnectTimeout;
			}
			if (redisOptions.SyncTimeout > 0)
			{
				configurationOptions.SyncTimeout = redisOptions.SyncTimeout;
			}

			configurationOptions.AbortOnConnectFail = redisOptions.AbortOnConnectFail;
			configurationOptions.AllowAdmin = redisOptions.AllowAdmin;

			// SSL：避免把 connection string 里的 ssl=true 覆盖成默认 ssl=false，所以只在显示配置时覆盖。
			if (redisOptions.Ssl || !string.IsNullOrWhiteSpace(redisOptions.SslHost))
			{
				configurationOptions.Ssl = redisOptions.Ssl;
				configurationOptions.SslHost = redisOptions.SslHost;
			}

			return ConnectionMultiplexer.Connect(configurationOptions);
		}
	}

	private bool _disposedValue;

	public void Dispose()
	{
		// 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposedValue) return;

		if (disposing)
		{
			// 避免未真正初始化时强制触发 Lazy.Value 连接创建。
			if (_lazyConnectionMultiplexer.IsValueCreated)
			{
				_lazyConnectionMultiplexer.Value.Dispose();
			}
		}

		_disposedValue = true;
	}
}
