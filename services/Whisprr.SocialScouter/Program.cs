using Whisprr.BlueskyService;
using Whisprr.Infrastructure.Redis;
using Whisprr.SocialScouter.Modules.RabbitMQ;
using Whisprr.SocialScouter.Modules.SocialListener;

var builder = Host.CreateApplicationBuilder(args);

builder
    .AddRedis()
    .AddBlueskyServices()
    .AddRabbitMQ()
    .AddSocialListenerServices();

var host = builder.Build();
host.Run();
