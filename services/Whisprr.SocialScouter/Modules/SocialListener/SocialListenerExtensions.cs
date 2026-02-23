using System.Threading.Channels;
using Whisprr.Entities.Models;
using Whisprr.SocialScouter.Modules.SocialListener.Bluesky;
using Whisprr.SocialScouter.Workers;

namespace Whisprr.SocialScouter.Modules.SocialListener;

/// <summary>
/// Extension methods for registering SocialListener services.
/// </summary>
public static class SocialListenerExtensions
{
    /// <summary>
    /// Adds channels for SocialListener communication and registers the workers.
    /// </summary>
    public static IHostApplicationBuilder AddSocialListenerServices(this IHostApplicationBuilder builder)
    {
        // Input channel: SocialTopicListeningTask (consumed by SocialListenerWorker)
        builder.Services.AddSingleton<Channel<SocialTopicListeningTask>>(_ =>
            Channel.CreateUnbounded<SocialTopicListeningTask>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            }));

        // Output channel: SocialInfo (produced by SocialListenerWorker, consumed by SocialInfoProcessorWorker)
        builder.Services.AddSingleton<Channel<SocialInfo>>(_ =>
            Channel.CreateUnbounded<SocialInfo>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            }));

        // Register channel readers/writers for easy injection
        builder.Services.AddSingleton(sp => sp.GetRequiredService<Channel<SocialTopicListeningTask>>().Reader);
        builder.Services.AddSingleton(sp => sp.GetRequiredService<Channel<SocialTopicListeningTask>>().Writer);
        builder.Services.AddSingleton(sp => sp.GetRequiredService<Channel<SocialInfo>>().Reader);
        builder.Services.AddSingleton(sp => sp.GetRequiredService<Channel<SocialInfo>>().Writer);

        // Register workers
        builder.Services.AddHostedService<SocialListenerWorker>();
        builder.Services.AddHostedService<SocialInfoProcessorWorker>();

        // Register listeners
        builder.Services.AddScoped<ISocialListener, BlueskySocialListener>();

        return builder;
    }
}
