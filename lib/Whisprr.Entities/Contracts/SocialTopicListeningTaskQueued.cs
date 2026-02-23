namespace Whisprr.Entities.Contracts;

/// <summary>
/// Contract for newly queued listening task
/// </summary>
public record SocialTopicListeningTaskQueued
{
  public Guid Id { get; init; }
  public Guid SocialTopicId { get; init; }
  public string Query { get; init; } = null!;
  public Guid SourcePlatformId { get; init; }
  public DateTimeOffset CreatedAt { get; init; }
}