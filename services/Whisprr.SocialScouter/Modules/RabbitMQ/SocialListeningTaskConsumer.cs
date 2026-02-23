using System.Threading.Channels;
using StackExchange.Redis;
using Whisprr.Entities.Models;
using Whisprr.Infrastructure.RabbitMQ.Config;
using Whisprr.Infrastructure.RabbitMQ.Consumer;

namespace Whisprr.SocialScouter.Modules.RabbitMQ;

/// <summary>
/// Hosted service that consumes social listening tasks from RabbitMQ
/// and pushes them to the in-memory channel for processing by SocialListenerWorker.
/// Includes Dead Letter Exchange (DLX) support for failed messages.
/// </summary>
public sealed partial class SocialListeningTaskConsumer : RabbitMQConsumerBase<SocialTopicListeningTask>
{
    private readonly ChannelWriter<SocialTopicListeningTask> _taskChannelWriter;

    public SocialListeningTaskConsumer(
        RabbitMQConnectionManager connectionManager,
        RabbitMQOptions options,
        ChannelWriter<SocialTopicListeningTask> taskChannelWriter,
        IDatabase redisDb,
        ILogger<SocialListeningTaskConsumer> logger)
        : base(connectionManager, options, redisDb, logger)
    {
        _taskChannelWriter = taskChannelWriter;
    }

    protected override string QueueName => Options.ListeningTaskQueue;
    protected override string ExchangeName => Options.ListeningTaskExchange;
    protected override string RoutingKey => Options.ListeningTaskRoutingKey;
    protected override ChannelWriter<SocialTopicListeningTask> ChannelWriter => _taskChannelWriter;

    private RabbitMQOptions Options => _options;
}
