using BlueskyService.Models.Domain;

namespace BlueskyService.Modules.BlueskySessionStore;

/// <summary>
/// The program that uses this library must implement this to persist
/// our session tokens. Recommended: redis.
/// </summary>
public interface IBlueskySessionStore
{
  Task<BlueskySession?> GetSessionAsync();
  Task SaveSessionAsync(BlueskySession session);
}