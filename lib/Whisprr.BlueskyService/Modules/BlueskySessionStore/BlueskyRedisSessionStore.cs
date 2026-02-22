
using StackExchange.Redis;
using Whisprr.BlueskyService.Models.Domain;
using Whisprr.BlueskyService.Models.Extensions;

namespace Whisprr.BlueskyService.Modules.BlueskySessionStore;

public class BlueskyRedisSessionStore(IDatabase db) : IBlueskySessionStore
{
  private string _sessionKey = "bluesky-session";

  public async Task SaveSessionAsync(BlueskySession session)
  {
    var hash = session.ToHash();
    await db.HashSetAsync(_sessionKey, hash);
  }

  public async Task<BlueskySession?> GetSessionAsync()
  {
    var hashFields = await db.HashGetAllAsync(_sessionKey);

    return hashFields.ToBlueskySession();
  }
}