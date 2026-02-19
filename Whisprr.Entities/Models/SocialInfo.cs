using System.ComponentModel.DataAnnotations;
using Whisprr.Entities.Enums;

namespace Whisprr.Entities.Models;

/// <summary>
/// Describes a social information from a post, timeline, feed, etc.
/// </summary>
public class SocialInfo
{
  public Guid Id { get; set; }
  [Required]
  [MaxLength(100)]
  public required string Title { get; set; }
  [Required]
  [MaxLength(1_000)]
  public required string Content { get; set; }
  public DateTimeOffset CreatedAt { get; set; }
  public Sentiment Sentiment { get; set; }
  [Url]
  [Required]
  public required string OriginalUrl { get; set; }
  /// <summary>
  /// PK for <see cref="SourcePlatform"/>
  /// </summary>
  [Required]
  public Guid SourcePlatformId { get; set; }
  /// <summary>
  /// Reference navigation to populate the data when fetched
  /// </summary>
  public SourcePlatform? SourcePlatform { get; set; }
}