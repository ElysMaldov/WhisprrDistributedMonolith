using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Modules.BlueskyAuthService
{
  public interface IBlueskyAuthService
  {
    public Task<BlueskySession> CreateSession(string bskyHandle, string bskyPassword);
    public Task<BlueskySession> RefreshSession(string refreshToken);
  }
}