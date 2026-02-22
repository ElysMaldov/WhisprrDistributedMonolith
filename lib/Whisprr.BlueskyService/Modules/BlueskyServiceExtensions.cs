using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Whisprr.BlueskyService.Modules.BlueskyAuthHandler;
using Whisprr.BlueskyService.Modules.BlueskySessionStore;
using BlueskyAuthServiceImpl = Whisprr.BlueskyService.Modules.BlueskyAuthService.BlueskyAuthService;
using BlueskyServiceImpl = Whisprr.BlueskyService.Modules.BlueskyService.BlueskyService;
using IBlueskyServiceInterface = Whisprr.BlueskyService.Modules.BlueskyService.IBlueskyService;
using IBlueskyAuthServiceInterface = Whisprr.BlueskyService.Modules.BlueskyAuthService.IBlueskyAuthService;

namespace Whisprr.BlueskyService;

/// <summary>
/// Extension methods for registering Bluesky services.
/// </summary>
public static class BlueskyServiceExtensions
{
    /// <summary>
    /// Adds Bluesky service infrastructure including HttpClients, auth handlers, and session store.
    /// </summary>
    public static IHostApplicationBuilder AddBlueskyServices(this IHostApplicationBuilder builder)
    {
        var baseUrl = builder.Configuration["Bluesky:BaseUrl"] ?? "https://api.bsky.app";

        // Auth handlers
        builder.Services.AddTransient<BlueskyAuthHandler>();
        builder.Services.AddTransient<BlueskyRefreshSessionHandler>();

        // BlueskyService with auth handlers
        builder.Services.AddHttpClient<IBlueskyServiceInterface, BlueskyServiceImpl>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        })
        .AddHttpMessageHandler<BlueskyAuthHandler>()
        .AddHttpMessageHandler<BlueskyRefreshSessionHandler>();

        // BlueskyAuthService (no auth handlers needed)
        builder.Services.AddHttpClient<IBlueskyAuthServiceInterface, BlueskyAuthServiceImpl>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        });

        // Session store (requires Redis to be registered first)
        builder.Services.AddSingleton<IBlueskySessionStore, BlueskyRedisSessionStore>();

        return builder;
    }
}
