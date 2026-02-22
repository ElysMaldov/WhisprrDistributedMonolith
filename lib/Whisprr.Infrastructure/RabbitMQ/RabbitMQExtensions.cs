using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace Whisprr.Infrastructure.RabbitMQ;

/// <summary>
/// Extension methods for registering RabbitMQ infrastructure services.
/// </summary>
public static class RabbitMQExtensions
{
    /// <summary>
    /// Adds shared RabbitMQ infrastructure services to the DI container.
    /// </summary>
    public static IServiceCollection AddRabbitMQInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(RabbitMQOptions.SectionName)
            .Get<RabbitMQOptions>() ?? new RabbitMQOptions();

        services.AddSingleton(options);

        // Create and register the connection as a singleton
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RabbitMQConnectionManager>>();

            var connectionFactory = new ConnectionFactory
            {
                HostName = options.Host,
                Port = options.Port,
                UserName = options.Username,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                ConsumerDispatchConcurrency = 1,
            };

            var connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            return new RabbitMQConnectionManager(connection, logger);
        });

        return services;
    }

    /// <summary>
    /// Serializes an object to JSON bytes.
    /// </summary>
    public static byte[] ToJsonBytes<T>(this T obj)
    {
        return JsonSerializer.SerializeToUtf8Bytes(obj, JsonOptions.Default);
    }

    /// <summary>
    /// Deserializes JSON bytes to an object.
    /// </summary>
    public static T? FromJsonBytes<T>(this ReadOnlySpan<byte> bytes)
    {
        return JsonSerializer.Deserialize<T>(bytes, JsonOptions.Default);
    }

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }
}
