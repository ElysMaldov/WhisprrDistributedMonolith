using RabbitMQ.Client;
using Whisprr.Infrastructure.RabbitMQ;

namespace Whisprr.SocialScouter.Modules.RabbitMQ;

/// <summary>
/// Publishes social listening tasks to RabbitMQ.
/// Other services (e.g., API) can use this to trigger listening tasks.
/// </summary>
public sealed partial class RabbitMQListeningTaskPublisher : IAsyncDisposable
{
    private readonly RabbitMQConnectionManager _connectionManager;
    private readonly RabbitMQOptions _options;
    private readonly ILogger<RabbitMQListeningTaskPublisher> _logger;
    private IChannel? _channel;

    public RabbitMQListeningTaskPublisher(
        RabbitMQConnectionManager connectionManager,
        RabbitMQOptions options,
        ILogger<RabbitMQListeningTaskPublisher> logger)
    {
        _connectionManager = connectionManager;
        _options = options;
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

            // Ensure exchange exists
            await _channel.ExchangeDeclareAsync(
                exchange: _options.ListeningTaskExchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            LogChannelInitialized(_logger, _options.ListeningTaskExchange);
        }
    }

    /// <summary>
    /// Publishes a listening task to RabbitMQ.
    /// </summary>
    public async Task PublishAsync<T>(T task, CancellationToken cancellationToken = default)
    {
        await EnsureChannelAsync(cancellationToken);

        if (_channel is null)
        {
            throw new InvalidOperationException("Channel not initialized");
        }

        var body = task.ToJsonBytes();

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
        };

        await _channel.BasicPublishAsync(
            exchange: _options.ListeningTaskExchange,
            routingKey: _options.ListeningTaskRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);

        LogTaskPublished(_logger, typeof(T).Name);
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
        Message = "Publisher channel initialized. Exchange: {Exchange}")]
    static partial void LogChannelInitialized(ILogger<RabbitMQListeningTaskPublisher> logger, string exchange);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Task published to RabbitMQ. Type: {TaskType}")]
    static partial void LogTaskPublished(ILogger<RabbitMQListeningTaskPublisher> logger, string taskType);
}
