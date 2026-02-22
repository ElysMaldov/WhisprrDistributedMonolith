using System.Threading.Channels;
using Whisprr.Entities.Models;
using Whisprr.SocialScouter.Modules.SocialListener;

namespace Whisprr.SocialScouter.Workers;

/// <summary>
/// Worker that consumes listening tasks from the input channel,
/// executes social listeners, and produces SocialInfo to the output channel.
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
            var listener = scope.ServiceProvider.GetRequiredService<ISocialListener>();

            LogProcessingTask(logger, task.Id, task.SocialTopic.Id);

            // Execute the search
            var socialInfos = await listener.Search(task);

            // Push each SocialInfo to the output channel one by one
            foreach (var socialInfo in socialInfos)
            {
                await socialInfoChannelWriter.WriteAsync(socialInfo, stoppingToken);
                LogPushedSocialInfo(logger, socialInfo.Id);
            }

            LogTaskCompleted(logger, task.Id, socialInfos.Length);
        }
        catch (Exception ex)
        {
            LogTaskFailed(logger, ex, task.Id);
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
        Message = "Pushed SocialInfo {SocialInfoId} to channel")]
    static partial void LogPushedSocialInfo(ILogger<SocialListenerWorker> logger, Guid socialInfoId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Task {TaskId} completed. Found {Count} results")]
    static partial void LogTaskCompleted(ILogger<SocialListenerWorker> logger, Guid taskId, int count);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to process task {TaskId}")]
    static partial void LogTaskFailed(ILogger<SocialListenerWorker> logger, Exception ex, Guid taskId);
}
