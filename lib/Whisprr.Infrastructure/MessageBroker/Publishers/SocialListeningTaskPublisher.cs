using MassTransit;
using Whisprr.Entities.Contracts;

namespace Whisprr.Infrastructure.MessageBroker.Publishers;

public sealed class SocialListeningTaskPublisher(IPublishEndpoint publishEndpoint)
{
  // TODO use hangfire to run every 15 minutes
  public async Task Publish(CancellationToken ct = default)
  {
    // TODO fetch and join data from db
    await publishEndpoint.Publish(new SocialTopicListeningTaskQueued
    {
      Id = NewId.NextGuid(), // MassTransit's NewId is faster than Guid.CreateVersion7()
      SocialTopicId = NewId.NextGuid(),
      SourcePlatformId = NewId.NextGuid(),
      Query = "Minimax M2.5",
      CreatedAt = DateTimeOffset.UtcNow
    }, ct);
  }
}