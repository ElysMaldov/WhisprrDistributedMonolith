using System.Globalization;

namespace Whisprr.Entities.Models;

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
}