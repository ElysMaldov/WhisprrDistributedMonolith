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

    // Dead Letter Exchange (DLX) settings

    /// <summary>
    /// Exchange name for dead letter messages.
    /// </summary>
    public string DeadLetterExchange { get; set; } = "social.listening.dlx";

    /// <summary>
    /// Queue name for dead letter messages.
    /// </summary>
    public string DeadLetterQueue { get; set; } = "social.listening.tasks.dlq";

    /// <summary>
    /// Routing key for dead letter messages.
    /// </summary>
    public string DeadLetterRoutingKey { get; set; } = "task.failed";

    /// <summary>
    /// Maximum number of retry attempts before sending to DLQ.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Simulate processing failures for testing DLX (0 = disabled, 1 = always fail, 0.5 = 50% fail).
    /// </summary>
    public double SimulateFailureRate { get; set; } = 0;
}
