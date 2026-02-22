namespace Whisprr.Infrastructure.RabbitMQ;

/// <summary>
/// Configuration options for RabbitMQ connection.
/// </summary>
public sealed class RabbitMQOptions
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "whisprr";
    public string Password { get; set; } = "whisprr";
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Queue name for social listening tasks.
    /// </summary>
    public string ListeningTaskQueue { get; set; } = "social.listening.tasks";

    /// <summary>
    /// Exchange name for social listening tasks.
    /// </summary>
    public string ListeningTaskExchange { get; set; } = "social.listening";

    /// <summary>
    /// Routing key for social listening tasks.
    /// </summary>
    public string ListeningTaskRoutingKey { get; set; } = "task.new";
}
