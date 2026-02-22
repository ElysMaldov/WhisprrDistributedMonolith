using StackExchange.Redis;
using Whisprr.BlueskyService.Modules.BlueskyAuthHandler;
using Whisprr.BlueskyService.Modules.BlueskyAuthService;
using Whisprr.BlueskyService.Modules.BlueskyService;
using Whisprr.BlueskyService.Modules.BlueskySessionStore;
using Whisprr.SocialScouter;
using Whisprr.SocialScouter.Modules.SocialListener;

var builder = Host.CreateApplicationBuilder(args);

// Configure Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

// Inject session store
builder.Services.AddSingleton<IBlueskySessionStore, BlueskyRedisSessionStore>();

// Get Bluesky configuration
var blueskyBaseUrl = builder.Configuration["Bluesky:BaseUrl"] ?? "https://api.bsky.app";

// Inject auth handlers
builder.Services.AddTransient<BlueskyAuthHandler>();
builder.Services.AddTransient<BlueskyRefreshSessionHandler>();

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

// Inject BlueskyService
builder.Services.AddScoped<IBlueskyService, BlueskyService>();

// Inject SocialListener
builder.Services.AddScoped<ISocialListener, BlueskySocialListener>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
