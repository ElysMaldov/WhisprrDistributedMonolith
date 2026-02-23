namespace Whisprr.Infrastructure.RabbitMQ.Producer;

/// <summary>
/// Generic interface for publishing messages to RabbitMQ.
/// </summary>
public interface IRabbitMQProducer
{
    /// <summary>
    /// Produces a message to the specified exchange with the given routing key.
    /// </summary>
    Task ProduceAsync<T>(
        T message,
        string exchange,
        string routingKey,
        CancellationToken cancellationToken = default);
}
