namespace ST.Infra.Redis.Provider;

public interface IRedisClient : IDisposable
{
	IConnectionMultiplexer GetConnection();

	IDatabase GetDatabase();
}
