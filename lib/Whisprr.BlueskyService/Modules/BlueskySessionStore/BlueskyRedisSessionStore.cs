using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Modules.BlueskySessionStore;

public class BlueskyRedisSessionStore : IBlueskySessionStore
{
  public Task<BlueskySession?> GetSessionAsync()
  {
    throw new NotImplementedException();
  }

  public Task SaveSessionAsync(BlueskySession session)
  {
    throw new NotImplementedException();
  }
}