using System.Threading.Channels;
using StackExchange.Redis;
using Whisprr.BlueskyService.Modules.BlueskyAuthHandler;
using Whisprr.BlueskyService.Modules.BlueskyAuthService;
using Whisprr.BlueskyService.Modules.BlueskyService;
using Whisprr.BlueskyService.Modules.BlueskySessionStore;
using Whisprr.Entities.Models;
using Whisprr.SocialScouter.Modules.RabbitMQ;
using Whisprr.SocialScouter.Modules.SocialListener;
using Whisprr.SocialScouter.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Configure Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

// Inject session store
builder.Services.AddSingleton<IBlueskySessionStore, BlueskyRedisSessionStore>();

// Get Bluesky configuration
var blueskyBaseUrl = builder.Configuration["Bluesky:BaseUrl"] ?? "https://api.bsky.app";

// Inject HttpClient typed client for BlueskyService with auth handlers
// BlueskyAuthHandler adds the auth token to requests
// BlueskyRefreshSessionHandler handles 401 responses by refreshing the session
builder.Services.AddHttpClient<IBlueskyService, BlueskyService>(client =>
{
  client.BaseAddress = new Uri(blueskyBaseUrl);
})
.AddHttpMessageHandler<BlueskyAuthHandler>()
.AddHttpMessageHandler<BlueskyRefreshSessionHandler>();

// Inject HttpClient typed client for BlueskyAuthService (no auth handler needed for auth endpoints)
builder.Services.AddHttpClient<IBlueskyAuthService, BlueskyAuthService>(client =>
{
  client.BaseAddress = new Uri(blueskyBaseUrl);
});

// Inject auth handlers
builder.Services.AddTransient<BlueskyAuthHandler>();
builder.Services.AddTransient<BlueskyRefreshSessionHandler>();

// Inject BlueskyService
builder.Services.AddScoped<IBlueskyService, BlueskyService>();

// Inject SocialListener
builder.Services.AddScoped<ISocialListener, BlueskySocialListener>();

// Configure channels
// Channel for activating social listeners (input) - consumed by SocialListenerWorker
builder.Services.AddSingleton<Channel<SocialTopicListeningTask>>(_ =>
    Channel.CreateUnbounded<SocialTopicListeningTask>(new UnboundedChannelOptions
    {
      SingleReader = false,
      SingleWriter = false
    }));

// Channel for SocialInfo results (output) - one by one, single consumer
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

// Add RabbitMQ infrastructure (includes RabbitMQListeningTaskConsumer hosted service)
builder.AddRabbitMQ();

// Register workers
builder.Services.AddHostedService<SocialListenerWorker>();
builder.Services.AddHostedService<SocialInfoProcessorWorker>();

var host = builder.Build();
host.Run();
