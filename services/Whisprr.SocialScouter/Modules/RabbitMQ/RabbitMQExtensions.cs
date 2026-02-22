using Whisprr.Infrastructure.RabbitMQ;

namespace Whisprr.SocialScouter.Modules.RabbitMQ;

/// <summary>
/// Extension methods for registering RabbitMQ services in SocialScouter.
/// </summary>
public static class RabbitMQExtensions
{
    /// <summary>
    /// Adds RabbitMQ infrastructure and SocialScouter-specific services to the DI container.
    /// </summary>
    public static IHostApplicationBuilder AddRabbitMQ(this IHostApplicationBuilder builder)
    {
        // Add shared infrastructure
        builder.Services.AddRabbitMQInfrastructure(builder.Configuration);

        // Register SocialScouter-specific services
        builder.Services.AddHostedService<RabbitMQListeningTaskConsumer>();
        builder.Services.AddSingleton<RabbitMQListeningTaskPublisher>();

        return builder;
    }
}
