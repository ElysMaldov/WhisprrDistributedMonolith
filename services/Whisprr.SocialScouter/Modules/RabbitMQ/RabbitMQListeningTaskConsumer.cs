using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using Whisprr.Entities.Models;
using Whisprr.Infrastructure.RabbitMQ;

namespace Whisprr.SocialScouter.Modules.RabbitMQ;

/// <summary>
/// Hosted service that consumes social listening tasks from RabbitMQ
/// and pushes them to the in-memory channel for processing by SocialListenerWorker.
/// Includes Dead Letter Exchange (DLX) support for failed messages.
/// </summary>
public sealed partial class RabbitMQListeningTaskConsumer : BackgroundService
{
    private readonly RabbitMQConnectionManager _connectionManager;
    private readonly RabbitMQOptions _options;
    private readonly ChannelWriter<SocialTopicListeningTask> _taskChannelWriter;
    private readonly IDatabase _redisDb;
    private readonly ILogger<RabbitMQListeningTaskConsumer> _logger;

    private IChannel? _channel;
    private string? _consumerTag;

    // Redis key prefix for retry counting
    private const string RetryCountPrefix = "rabbitmq:retry:";
    // Retry count expiration (24 hours)
    private static readonly TimeSpan RetryCountExpiration = TimeSpan.FromHours(24);

    public RabbitMQListeningTaskConsumer(
        RabbitMQConnectionManager connectionManager,
        RabbitMQOptions options,
        ChannelWriter<SocialTopicListeningTask> taskChannelWriter,
        IDatabase redisDb,
        ILogger<RabbitMQListeningTaskConsumer> logger)
    {
        _connectionManager = connectionManager;
        _options = options;
        _taskChannelWriter = taskChannelWriter;
        _redisDb = redisDb;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogConsumerStarting(_logger, _options.Host, _options.ListeningTaskQueue);

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
            exchange: _options.ListeningTaskExchange,
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
            queue: _options.ListeningTaskQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: _options.ListeningTaskQueue,
            exchange: _options.ListeningTaskExchange,
            routingKey: _options.ListeningTaskRoutingKey,
            cancellationToken: cancellationToken);

        // Set QoS to process one message at a time per consumer
        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: cancellationToken);

        LogChannelInitialized(_logger, _options.ListeningTaskQueue, _options.ListeningTaskExchange);
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
            queue: _options.ListeningTaskQueue,
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
            var task = args.Body.Span.FromJsonBytes<SocialTopicListeningTask>();

            if (task is null)
            {
                LogDeserializationFailed(_logger, deliveryTag);
                // Don't requeue - send to DLQ immediately
                await _channel.BasicRejectAsync(deliveryTag, requeue: false);
                return;
            }

            LogMessageReceived(_logger, task.Id, deliveryTag);

            // Simulate failure for testing DLX
            if (_options.SimulateFailureRate > 0 && Random.Shared.NextDouble() < _options.SimulateFailureRate)
            {
                throw new InvalidOperationException("Simulated processing failure for DLX testing");
            }

            // Push to the channel (wait if channel is full)
            await _taskChannelWriter.WriteAsync(task);

            // Acknowledge the message after successful processing
            await _channel.BasicAckAsync(deliveryTag, multiple: false);

            // Clear retry count on success
            await ClearRetryCountAsync(messageId);

            LogMessageProcessed(_logger, task.Id);
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
    static partial void LogConsumerStarting(ILogger<RabbitMQListeningTaskConsumer> logger, string host, string queue);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "RabbitMQ channel initialized. Queue: {Queue}, Exchange: {Exchange}")]
    static partial void LogChannelInitialized(ILogger<RabbitMQListeningTaskConsumer> logger, string queue, string exchange);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "RabbitMQ DLX initialized. Exchange: {DlxExchange}, Queue: {DlqQueue}")]
    static partial void LogDlxInitialized(ILogger<RabbitMQListeningTaskConsumer> logger, string dlxExchange, string dlqQueue);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "RabbitMQ consumer started with tag: {ConsumerTag}")]
    static partial void LogConsumerStarted(ILogger<RabbitMQListeningTaskConsumer> logger, string consumerTag);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Received message. TaskId: {TaskId}, DeliveryTag: {DeliveryTag}")]
    static partial void LogMessageReceived(ILogger<RabbitMQListeningTaskConsumer> logger, Guid taskId, ulong deliveryTag);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Message processed successfully. TaskId: {TaskId}")]
    static partial void LogMessageProcessed(ILogger<RabbitMQListeningTaskConsumer> logger, Guid taskId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to deserialize message. DeliveryTag: {DeliveryTag}")]
    static partial void LogDeserializationFailed(ILogger<RabbitMQListeningTaskConsumer> logger, ulong deliveryTag);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Channel closed while processing message. DeliveryTag: {DeliveryTag}")]
    static partial void LogChannelClosed(ILogger<RabbitMQListeningTaskConsumer> logger, ulong deliveryTag);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to process message. DeliveryTag: {DeliveryTag}, MessageId: {MessageId}")]
    static partial void LogMessageProcessingFailed(ILogger<RabbitMQListeningTaskConsumer> logger, Exception ex, ulong deliveryTag, string messageId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Retry attempt {RetryCount}/{MaxRetries} for message {MessageId}")]
    static partial void LogRetryAttempt(ILogger<RabbitMQListeningTaskConsumer> logger, string messageId, int retryCount, int maxRetries);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Max retries ({MaxRetries}) reached for message {MessageId}. Sending to DLQ. Error: {ErrorMessage}")]
    static partial void LogMaxRetriesReached(ILogger<RabbitMQListeningTaskConsumer> logger, string messageId, int maxRetries, string errorMessage);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to reject message. DeliveryTag: {DeliveryTag}")]
    static partial void LogRejectFailed(ILogger<RabbitMQListeningTaskConsumer> logger, Exception ex, ulong deliveryTag);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to cancel consumer. ConsumerTag: {ConsumerTag}")]
    static partial void LogCancelFailed(ILogger<RabbitMQListeningTaskConsumer> logger, Exception ex, string consumerTag);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "RabbitMQ consumer stopping...")]
    static partial void LogConsumerStopping(ILogger<RabbitMQListeningTaskConsumer> logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "RabbitMQ consumer stopped")]
    static partial void LogConsumerStopped(ILogger<RabbitMQListeningTaskConsumer> logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "RabbitMQ consumer error")]
    static partial void LogConsumerError(ILogger<RabbitMQListeningTaskConsumer> logger, Exception ex);
}
