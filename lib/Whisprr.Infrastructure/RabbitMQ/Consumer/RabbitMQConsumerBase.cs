using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using Whisprr.Infrastructure.RabbitMQ.Config;

namespace Whisprr.Infrastructure.RabbitMQ.Consumer;

/// <summary>
/// Base class for RabbitMQ consumers with Dead Letter Exchange (DLX) support and retry logic.
/// </summary>
public abstract partial class RabbitMQConsumerBase<TMessage> : BackgroundService where TMessage : class
{
    private readonly RabbitMQConnectionManager _connectionManager;
    protected readonly RabbitMQOptions _options;
    private readonly IDatabase _redisDb;
    private readonly ILogger _logger;

    private IChannel? _channel;
    private string? _consumerTag;

    // Redis key prefix for retry counting
    private const string RetryCountPrefix = "rabbitmq:retry:";
    // Retry count expiration (24 hours)
    private static readonly TimeSpan RetryCountExpiration = TimeSpan.FromHours(24);

    /// <summary>
    /// The queue name to consume from.
    /// </summary>
    protected abstract string QueueName { get; }

    /// <summary>
    /// The exchange name to bind to.
    /// </summary>
    protected abstract string ExchangeName { get; }

    /// <summary>
    /// The routing key to bind with.
    /// </summary>
    protected abstract string RoutingKey { get; }

    /// <summary>
    /// The channel writer to push processed messages to.
    /// </summary>
    protected abstract ChannelWriter<TMessage> ChannelWriter { get; }

    protected RabbitMQConsumerBase(
        RabbitMQConnectionManager connectionManager,
        RabbitMQOptions options,
        IDatabase redisDb,
        ILogger logger)
    {
        _connectionManager = connectionManager;
        _options = options;
        _redisDb = redisDb;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogConsumerStarting(_logger, _options.Host, QueueName);

        try
        {
            await InitializeChannelAsync(stoppingToken);
            await StartConsumingAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogConsumerStopping(_logger);
            throw;
        }
        catch (Exception ex)
        {
            LogConsumerError(_logger, ex);
            throw;
        }
    }

    private async Task InitializeChannelAsync(CancellationToken cancellationToken)
    {
        _channel = await _connectionManager.CreateChannelAsync(cancellationToken);

        // Declare main exchange
        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Declare Dead Letter Exchange (DLX)
        await _channel.ExchangeDeclareAsync(
            exchange: _options.DeadLetterExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Declare Dead Letter Queue (DLQ)
        await _channel.QueueDeclareAsync(
            queue: _options.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Bind DLQ to DLX
        await _channel.QueueBindAsync(
            queue: _options.DeadLetterQueue,
            exchange: _options.DeadLetterExchange,
            routingKey: _options.DeadLetterRoutingKey,
            cancellationToken: cancellationToken);

        // Declare main queue with DLX arguments
        var queueArgs = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", _options.DeadLetterExchange },
            { "x-dead-letter-routing-key", _options.DeadLetterRoutingKey }
        };

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey,
            cancellationToken: cancellationToken);

        // Set QoS to process one message at a time per consumer
        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: cancellationToken);

        LogChannelInitialized(_logger, QueueName, ExchangeName);
        LogDlxInitialized(_logger, _options.DeadLetterExchange, _options.DeadLetterQueue);
    }

    private async Task StartConsumingAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
        {
            throw new InvalidOperationException("Channel not initialized");
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        LogConsumerStarted(_logger, _consumerTag);

        // Keep the consumer running until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            consumer.ReceivedAsync -= OnMessageReceivedAsync;
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null) return;

        var deliveryTag = args.DeliveryTag;
        var messageId = args.BasicProperties.MessageId ?? deliveryTag.ToString();

        try
        {
            // Deserialize the message
            var message = args.Body.Span.FromJsonBytes<TMessage>();

            if (message is null)
            {
                LogDeserializationFailed(_logger, deliveryTag);
                // Don't requeue - send to DLQ immediately
                await _channel.BasicRejectAsync(deliveryTag, requeue: false);
                return;
            }

            LogMessageReceived(_logger, messageId, deliveryTag);

            // Simulate failure for testing DLX
            if (_options.SimulateFailureRate > 0 && Random.Shared.NextDouble() < _options.SimulateFailureRate)
            {
                throw new InvalidOperationException("Simulated processing failure for DLX testing");
            }

            // Push to the channel (wait if channel is full)
            await ChannelWriter.WriteAsync(message);

            // Acknowledge the message after successful processing
            await _channel.BasicAckAsync(deliveryTag, multiple: false);

            // Clear retry count on success
            await ClearRetryCountAsync(messageId);

            LogMessageProcessed(_logger, messageId);
        }
        catch (ChannelClosedException)
        {
            // Channel was closed, don't requeue - message will be redelivered
            LogChannelClosed(_logger, deliveryTag);
        }
        catch (Exception ex)
        {
            LogMessageProcessingFailed(_logger, ex, deliveryTag, messageId);

            // Handle retry logic with DLX
            await HandleRetryAsync(deliveryTag, messageId, ex.Message);
        }
    }

    private async Task HandleRetryAsync(ulong deliveryTag, string messageId, string errorMessage)
    {
        if (_channel is null) return;

        try
        {
            // Get current retry count from Redis
            var retryCount = await GetRetryCountAsync(messageId);
            retryCount++;

            LogRetryAttempt(_logger, messageId, retryCount, _options.MaxRetryAttempts);

            if (retryCount >= _options.MaxRetryAttempts)
            {
                // Max retries reached - reject without requeue (will go to DLX)
                LogMaxRetriesReached(_logger, messageId, _options.MaxRetryAttempts, errorMessage);
                await _channel.BasicRejectAsync(deliveryTag, requeue: false);
                await ClearRetryCountAsync(messageId);
            }
            else
            {
                // Increment retry count and requeue
                await SetRetryCountAsync(messageId, retryCount);
                await _channel.BasicRejectAsync(deliveryTag, requeue: true);
            }
        }
        catch (Exception rejectEx)
        {
            LogRejectFailed(_logger, rejectEx, deliveryTag);
        }
    }

    private async Task<int> GetRetryCountAsync(string messageId)
    {
        var key = RetryCountPrefix + messageId;
        var value = await _redisDb.StringGetAsync(key);
        return value.IsNullOrEmpty ? 0 : (int)value;
    }

    private async Task SetRetryCountAsync(string messageId, int count)
    {
        var key = RetryCountPrefix + messageId;
        await _redisDb.StringSetAsync(key, count, RetryCountExpiration);
    }

    private async Task ClearRetryCountAsync(string messageId)
    {
        var key = RetryCountPrefix + messageId;
        await _redisDb.KeyDeleteAsync(key);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        LogConsumerStopping(_logger);

        if (_channel is not null && _consumerTag is not null)
        {
            try
            {
                await _channel.BasicCancelAsync(_consumerTag, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                LogCancelFailed(_logger, ex, _consumerTag);
            }
        }

        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }

        await base.StopAsync(cancellationToken);

        LogConsumerStopped(_logger);
    }

    // LoggerMessage source-generated methods
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "RabbitMQ consumer starting. Host: {Host}, Queue: {Queue}")]
    static partial void LogConsumerStarting(ILogger logger, string host, string queue);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "RabbitMQ channel initialized. Queue: {Queue}, Exchange: {Exchange}")]
    static partial void LogChannelInitialized(ILogger logger, string queue, string exchange);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "RabbitMQ DLX initialized. Exchange: {DlxExchange}, Queue: {DlqQueue}")]
    static partial void LogDlxInitialized(ILogger logger, string dlxExchange, string dlqQueue);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "RabbitMQ consumer started with tag: {ConsumerTag}")]
    static partial void LogConsumerStarted(ILogger logger, string consumerTag);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Received message. MessageId: {MessageId}, DeliveryTag: {DeliveryTag}")]
    static partial void LogMessageReceived(ILogger logger, string messageId, ulong deliveryTag);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Message processed successfully. MessageId: {MessageId}")]
    static partial void LogMessageProcessed(ILogger logger, string messageId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to deserialize message. DeliveryTag: {DeliveryTag}")]
    static partial void LogDeserializationFailed(ILogger logger, ulong deliveryTag);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Channel closed while processing message. DeliveryTag: {DeliveryTag}")]
    static partial void LogChannelClosed(ILogger logger, ulong deliveryTag);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to process message. DeliveryTag: {DeliveryTag}, MessageId: {MessageId}")]
    static partial void LogMessageProcessingFailed(ILogger logger, Exception ex, ulong deliveryTag, string messageId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Retry attempt {RetryCount}/{MaxRetries} for message {MessageId}")]
    static partial void LogRetryAttempt(ILogger logger, string messageId, int retryCount, int maxRetries);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Max retries ({MaxRetries}) reached for message {MessageId}. Sending to DLQ. Error: {ErrorMessage}")]
    static partial void LogMaxRetriesReached(ILogger logger, string messageId, int maxRetries, string errorMessage);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to reject message. DeliveryTag: {DeliveryTag}")]
    static partial void LogRejectFailed(ILogger logger, Exception ex, ulong deliveryTag);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to cancel consumer. ConsumerTag: {ConsumerTag}")]
    static partial void LogCancelFailed(ILogger logger, Exception ex, string consumerTag);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "RabbitMQ consumer stopping...")]
    static partial void LogConsumerStopping(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "RabbitMQ consumer stopped")]
    static partial void LogConsumerStopped(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "RabbitMQ consumer error")]
    static partial void LogConsumerError(ILogger logger, Exception ex);
}
