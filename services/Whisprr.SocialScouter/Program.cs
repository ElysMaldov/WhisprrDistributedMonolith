using Whisprr.BlueskyService;
using Whisprr.Infrastructure.MessageBroker;
using Whisprr.Infrastructure.Redis;
using Whisprr.SocialScouter.Modules.SocialListener;

var builder = Host.CreateApplicationBuilder(args);

builder
    .AddRedis()
    .AddBlueskyServices()
    .AddMessageBroker()
    .AddSocialListenerServices();

var host = builder.Build();
host.Run();
