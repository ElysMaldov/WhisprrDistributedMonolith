namespace Whisprr.Infrastructure.RabbitMQ;

/// <summary>
/// Generic interface for publishing messages to RabbitMQ.
/// </summary>
public interface IRabbitMQPublisher
{
    /// <summary>
    /// Publishes a message to the specified exchange with the given routing key.
    /// </summary>
    Task PublishAsync<T>(
        T message,
        string exchange,
        string routingKey,
        CancellationToken cancellationToken = default);
}
