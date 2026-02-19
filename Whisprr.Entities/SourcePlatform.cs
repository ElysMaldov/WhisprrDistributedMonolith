using System.ComponentModel.DataAnnotations;

namespace Whisprr.Entities;

/// <summary>
/// Describes a platform we use as a source (e.g. Bluesky, Mastodon, etc.)
/// </summary>
public class SourcePlatform
{
  public Guid Id { get; set; }
  [Required]
  [MaxLength(100)]
  public required string Name { get; set; }
  [Required]
  [Url]
  public required string SourceUrl { get; set; }
  [Required]
  [Url]
  public required string IconUrl { get; set; }
  /// <summary>
  /// Collection navigation
  /// </summary>
  public ICollection<SocialInfo> SocialInfos { get; } = [];
}