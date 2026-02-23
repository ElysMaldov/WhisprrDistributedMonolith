using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Whisprr.Infrastructure.Redis;

/// <summary>
/// Extension methods for registering Redis services.
/// </summary>
public static class RedisExtensions
{
    /// <summary>
    /// Adds Redis (StackExchange.Redis) to the DI container.
    /// </summary>
    public static IHostApplicationBuilder AddRedis(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        
        builder.Services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(connectionString));
        
        builder.Services.AddSingleton<IDatabase>(
            sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

        return builder;
    }
}
