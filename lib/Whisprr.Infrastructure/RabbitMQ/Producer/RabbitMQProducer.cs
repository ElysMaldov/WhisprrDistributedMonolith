using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Whisprr.Infrastructure.RabbitMQ.Config;

namespace Whisprr.Infrastructure.RabbitMQ.Producer;

/// <summary>
/// Generic RabbitMQ producer that can produce messages to any exchange.
/// </summary>
public sealed partial class RabbitMQProducer : IRabbitMQProducer, IAsyncDisposable
{
    private readonly RabbitMQConnectionManager _connectionManager;
    private readonly ILogger<RabbitMQProducer> _logger;
    private IChannel? _channel;

    public RabbitMQProducer(
        RabbitMQConnectionManager connectionManager,
        ILogger<RabbitMQProducer> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>
    /// Ensures the channel is initialized.
    /// </summary>
    private async Task EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is null || _channel.IsClosed)
        {
            _channel = await _connectionManager.CreateChannelAsync(cancellationToken);
            LogChannelInitialized(_logger);
        }
    }

    /// <summary>
    /// Produces a message to the specified exchange with the given routing key.
    /// </summary>
    public async Task ProduceAsync<T>(
        T message,
        string exchange,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        await EnsureChannelAsync(cancellationToken);

        if (_channel is null)
        {
            throw new InvalidOperationException("Channel not initialized");
        }

        // Ensure exchange exists (idempotent operation)
        await _channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var body = message.ToJsonBytes();

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
        };

        await _channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);

        LogMessageProduced(_logger, typeof(T).Name, exchange, routingKey);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Producer channel initialized")]
    static partial void LogChannelInitialized(ILogger<RabbitMQProducer> logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Message produced to RabbitMQ. Type: {MessageType}, Exchange: {Exchange}, RoutingKey: {RoutingKey}")]
    static partial void LogMessageProduced(ILogger<RabbitMQProducer> logger, string messageType, string exchange, string routingKey);
}
