using System.Globalization;

namespace Whisprr.Entities.Models;

/// <summary>
/// A topic users can listen to and receive multipel <see cref="SocialInfo"/>
/// </summary>
public class SocialTopic
{
  public Guid Id { get; set; }

  private string[] _keywords = [];

  /// <summary>
  /// Must be an array of non-empty strings
  /// </summary>
  public string[] Keywords
  {
    get => _keywords;
    set => _keywords = value
        .Where(k => !string.IsNullOrWhiteSpace(k))
        .ToArray();
  }

  /// <summary>
  /// Stored as a BCP-47 string in the DB, but used as CultureInfo in code.
  /// </summary>
  public required CultureInfo Language { get; set; }

  // TODO add many to many relationship with Users
  // TODO add many to many relationship with SocialInfo
}