namespace ST.Infra.EventBus.RabbitMQ.Internal;

public interface IRabbitMqPersistentConnection : IDisposable
{
	bool IsConnected { get; }

	bool TryConnect();

	IModel CreateModel();
}

