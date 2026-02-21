using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Whisprr.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Whisprr.Infrastructure.Persistence;

public static class PersistenceExtensions
{
  /// <summary>
  /// Use method extensions to easily setup our AppDbContext without cluttering Program.cs too much
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <returns></returns>
  public static IServiceCollection AddWhisprrPersistence(this IServiceCollection services, IConfiguration configuration)
  {
    var connectionString = configuration.GetConnectionString("DefaultConnection")
                          ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");

    // Build the data source and map the enums here
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

    dataSourceBuilder.MapEnum<Sentiment>();
    dataSourceBuilder.MapEnum<TaskProgressStatus>();

    var dataSource = dataSourceBuilder.Build();

    // Register the DbContext using the data source
    services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(dataSource));

    return services;
  }
}