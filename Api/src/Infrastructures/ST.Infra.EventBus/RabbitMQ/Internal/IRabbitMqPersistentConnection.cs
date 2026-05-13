namespace ST.Infra.EventBus.RabbitMQ.Internal;

public interface IRabbitMqPersistentConnection : IDisposable
{
	bool IsConnected { get; }

	bool TryConnect();

	Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default);
}
