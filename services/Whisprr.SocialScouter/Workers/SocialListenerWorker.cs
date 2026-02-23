using System.Threading.Channels;
using Whisprr.Entities.Models;
using Whisprr.SocialScouter.Modules.SocialListener;

namespace Whisprr.SocialScouter.Workers;

/// <summary>
/// Worker that consumes listening tasks from the input channel,
/// executes all registered social listeners in parallel, and produces SocialInfo to the output channel.
/// </summary>
public partial class SocialListenerWorker(
    ILogger<SocialListenerWorker> logger,
    IServiceScopeFactory scopeFactory,
    ChannelReader<SocialTopicListeningTask> taskChannelReader,
    ChannelWriter<SocialInfo> socialInfoChannelWriter) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(logger);

        await foreach (var task in taskChannelReader.ReadAllAsync(stoppingToken))
        {
            // Fire and forget - process each task independently
            _ = ProcessTaskAsync(task, stoppingToken);
        }
    }

    private async Task ProcessTaskAsync(SocialTopicListeningTask task, CancellationToken stoppingToken)
    {
        try
        {
            // Create a scope for scoped services (ISocialListener, IBlueskyService, etc.)
            using var scope = scopeFactory.CreateScope();
            var listeners = scope.ServiceProvider.GetRequiredService<IEnumerable<ISocialListener>>();

            LogProcessingTask(logger, task.Id, task.SocialTopic.Id);

            // Execute all listeners in parallel
            var listenerTasks = listeners.Select(listener => ExecuteListenerAsync(listener, task, stoppingToken));
            await Task.WhenAll(listenerTasks);

            LogTaskCompleted(logger, task.Id);
        }
        catch (Exception ex)
        {
            LogTaskFailed(logger, ex, task.Id);
        }
    }

    private async Task ExecuteListenerAsync(ISocialListener listener, SocialTopicListeningTask task, CancellationToken stoppingToken)
    {
        var listenerType = listener.GetType().Name;
        LogExecutingListener(logger, listenerType, task.Id);

        try
        {
            var socialInfos = await listener.Search(task);

            // Push each SocialInfo to the output channel one by one
            foreach (var socialInfo in socialInfos)
            {
                await socialInfoChannelWriter.WriteAsync(socialInfo, stoppingToken);
                LogPushedSocialInfo(logger, socialInfo.Id, listenerType);
            }

            LogListenerCompleted(logger, listenerType, task.Id, socialInfos.Length);
        }
        catch (Exception ex)
        {
            LogListenerFailed(logger, ex, listenerType, task.Id);
        }
    }

    // LoggerMessage source-generated methods to avoid boxing
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "SocialListenerWorker started. Waiting for listening tasks...")]
    static partial void LogWorkerStarted(ILogger<SocialListenerWorker> logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Processing task {TaskId} for topic {TopicId}")]
    static partial void LogProcessingTask(ILogger<SocialListenerWorker> logger, Guid taskId, Guid topicId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Executing listener {ListenerType} for task {TaskId}")]
    static partial void LogExecutingListener(ILogger<SocialListenerWorker> logger, string listenerType, Guid taskId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Pushed SocialInfo {SocialInfoId} to channel from {ListenerType}")]
    static partial void LogPushedSocialInfo(ILogger<SocialListenerWorker> logger, Guid socialInfoId, string listenerType);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Listener {ListenerType} completed for task {TaskId}. Found {Count} results")]
    static partial void LogListenerCompleted(ILogger<SocialListenerWorker> logger, string listenerType, Guid taskId, int count);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Listener {ListenerType} failed for task {TaskId}")]
    static partial void LogListenerFailed(ILogger<SocialListenerWorker> logger, Exception ex, string listenerType, Guid taskId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Task {TaskId} completed. All listeners executed.")]
    static partial void LogTaskCompleted(ILogger<SocialListenerWorker> logger, Guid taskId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to process task {TaskId}")]
    static partial void LogTaskFailed(ILogger<SocialListenerWorker> logger, Exception ex, Guid taskId);
}
