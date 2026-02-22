using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Modules.BlueskySessionStore;

/// <summary>
/// The program that uses this library must implement this to persist
/// our session tokens. Recommended: redis.
/// </summary>
public interface IBlueskySessionStore
{
  Task SaveSessionAsync(BlueskySession session);
  Task<BlueskySession?> GetSessionAsync();
}