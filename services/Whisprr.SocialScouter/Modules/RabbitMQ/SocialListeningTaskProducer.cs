using Whisprr.Entities.Models;
using Whisprr.Infrastructure.RabbitMQ.Config;
using Whisprr.Infrastructure.RabbitMQ.Producer;

namespace Whisprr.SocialScouter.Modules.RabbitMQ;

/// <summary>
/// Publishes social listening tasks to RabbitMQ.
/// Other services (e.g., API) can use this to trigger listening tasks.
/// </summary>
public sealed partial class SocialListeningTaskProducer : IAsyncDisposable
{
    private readonly IRabbitMQProducer _producer;
    private readonly RabbitMQOptions _options;
    private readonly ILogger<SocialListeningTaskProducer> _logger;

    public SocialListeningTaskProducer(
        IRabbitMQProducer producer,
        RabbitMQOptions options,
        ILogger<SocialListeningTaskProducer> logger)
    {
        _producer = producer;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Publishes a listening task to RabbitMQ.
    /// </summary>
    public async Task PublishAsync(SocialTopicListeningTask task, CancellationToken cancellationToken = default)
    {
        await _producer.ProduceAsync(
            task,
            _options.ListeningTaskExchange,
            _options.ListeningTaskRoutingKey,
            cancellationToken);

        LogTaskProduced(_logger, task.Id);
    }

    public async ValueTask DisposeAsync()
    {
        if (_producer is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Listening task produced to RabbitMQ. TaskId: {TaskId}")]
    static partial void LogTaskProduced(ILogger<SocialListeningTaskProducer> logger, Guid taskId);

}
