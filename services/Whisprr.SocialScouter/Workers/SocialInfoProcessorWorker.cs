using System.Threading.Channels;
using Whisprr.Entities.Models;

namespace Whisprr.SocialScouter.Workers;

/// <summary>
/// Worker that consumes SocialInfo from the channel for further processing.
/// Only one instance of this worker should run (SingleReader = true on the channel).
/// </summary>
public partial class SocialInfoProcessorWorker(
    ILogger<SocialInfoProcessorWorker> logger,
    ChannelReader<SocialInfo> socialInfoChannelReader) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(logger);

        await foreach (var socialInfo in socialInfoChannelReader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessSocialInfoAsync(socialInfo, stoppingToken);
            }
            catch (Exception ex)
            {
                LogProcessingFailed(logger, ex, socialInfo.Id);
            }
        }
    }

    private async Task ProcessSocialInfoAsync(SocialInfo socialInfo, CancellationToken stoppingToken)
    {
        LogProcessingSocialInfo(logger, socialInfo.Id, socialInfo.SourcePlatform?.Name);

        // TODO: Implement processing logic
        // Examples:
        // - Save to database
        // - Analyze sentiment
        // - Send notifications
        // - Forward to another service
        // - etc.

        var contentPreview = socialInfo.Content?[..Math.Min(50, socialInfo.Content?.Length ?? 0)];
        LogSocialInfoProcessed(logger, socialInfo.Id, contentPreview);

        await Task.CompletedTask;
    }

    // LoggerMessage source-generated methods to avoid boxing
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "SocialInfoProcessorWorker started. Waiting for SocialInfo...")]
    static partial void LogWorkerStarted(ILogger<SocialInfoProcessorWorker> logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Processing SocialInfo {SocialInfoId} from {Platform}")]
    static partial void LogProcessingSocialInfo(ILogger<SocialInfoProcessorWorker> logger, Guid socialInfoId, string? platform);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Processed SocialInfo {SocialInfoId}: {ContentPreview}")]
    static partial void LogSocialInfoProcessed(ILogger<SocialInfoProcessorWorker> logger, Guid socialInfoId, string? contentPreview);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to process SocialInfo {SocialInfoId}")]
    static partial void LogProcessingFailed(ILogger<SocialInfoProcessorWorker> logger, Exception ex, Guid socialInfoId);
}
