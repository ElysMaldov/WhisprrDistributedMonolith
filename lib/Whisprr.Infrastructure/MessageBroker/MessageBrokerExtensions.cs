using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Whisprr.Infrastructure.MessageBroker.Config;

namespace Whisprr.Infrastructure.MessageBroker;

/// <summary>
/// Extension methods for registering MessageBroker infrastructure builder.Services.
/// </summary>
public static class MessageBrokerExtensions
{
  public static IHostApplicationBuilder AddMessageBroker(this IHostApplicationBuilder builder)
  {
    var options = builder.Configuration
        .GetSection(MessageBrokerOptions.SectionName)
        .Get<MessageBrokerOptions>() ?? new MessageBrokerOptions();

    builder.Services.AddSingleton(options);

    builder.Services.AddMassTransit(busConfigurator =>
    {
      busConfigurator.SetKebabCaseEndpointNameFormatter();
      busConfigurator.UsingRabbitMq((context, configurator) =>
      {
        var hostUri = new Uri(options.Host);
        configurator.Host(hostUri, h =>
        {
          h.Username(options.Username);
          h.Password(options.Password);
        });
      });
    });

    return builder;
  }
}
