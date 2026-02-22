namespace Whisprr.Infrastructure.MessageBroker.Config;

public sealed class MessageBrokerOptions
{
  public const string SectionName = "MessageBroker";
  public string Host { get; set; } = "rabbitmq://localhost:5672";
  public string Username { get; set; } = "whisprr";
  public string Password { get; set; } = "whisprr";
}
