using StackExchange.Redis;
using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Models.Extensions;

/// <summary>
/// Seperating these methods so BlueskySession can be a pure record and not depend
/// on redis everytime.
/// </summary>
public static class BlueskyRedisExtensions
{
  public static HashEntry[] ToHash(this BlueskySession session) => [
      new(nameof(session.AccessToken), session.AccessToken),
      new(nameof(session.RefreshToken), session.RefreshToken)
  ];

  public static BlueskySession? ToBlueskySession(this HashEntry[] hash)
  {
    if (hash.Length == 0)
    {
      return null;
    }

    var entries = hash.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
    return new BlueskySession
    {
      AccessToken = entries[nameof(BlueskySession.AccessToken)],
      RefreshToken = entries[nameof(BlueskySession.RefreshToken)]
    };
  }
}