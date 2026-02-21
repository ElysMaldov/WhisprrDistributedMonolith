using Whisprr.Entities.Enums;
using Whisprr.Entities.Interfaces;

namespace Whisprr.Entities.Models;

public class SocialTopicListeningTask : ITrackableModel
{
  public Guid Id { get; set; }
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset? UpdatedAt { get; set; }

  public TaskProgressStatus Status { get; set; }

  public Guid SocialTopicId { get; set; }
  public SocialTopic SocialTopic { get; set; } = null!; // Use dammit to avoid this field being nullable by compiler, but will be populated by EF Core. Kind of like late in dart.

  public Guid SourcePlatformId { get; set; }
  public SourcePlatform SourcePlatform { get; set; } = null!;

  public ICollection<SocialInfo> GeneratedSocialInfos { get; set; } = []; // Since the name doesn't match convention, we setup the relationship using FLuent API in the context
}