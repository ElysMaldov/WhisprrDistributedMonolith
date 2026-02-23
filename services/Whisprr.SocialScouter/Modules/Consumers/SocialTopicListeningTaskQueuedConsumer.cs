using System.Threading.Channels;
using MassTransit;
using Whisprr.Entities.Contracts;
using Whisprr.Entities.Models;

namespace Whisprr.SocialScouter.Modules.Consumers;

public sealed class SocialTopicListeningTaskQueuedConsumer(
  ILogger<SocialTopicListeningTaskQueuedConsumer> logger,
  ChannelWriter<SocialTopicListeningTask> taskChannelWriter)
  : IConsumer<SocialTopicListeningTaskQueued>
{
  public async Task Consume(ConsumeContext<SocialTopicListeningTaskQueued> context)
  {
    var data = context.Message;
    SocialTopicListeningTask newTask = new()
    {
      Id = data.Id,
      CreatedAt = data.CreatedAt,
      SocialTopicId = data.SocialTopicId,
      SourcePlatformId = data.SourcePlatformId,
    };


    await taskChannelWriter.WriteAsync(newTask);
  }
}