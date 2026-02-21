using Whisprr.Entities.Models;

namespace Whisprr.SocialScouter.Modules.SocialListener;

interface ISocialListener
{
  public Task<SocialInfo[]> Search(SocialTopicListeningTask task);
}