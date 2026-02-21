using Whisprr.Entities.Models;

namespace Whisprr.SocialScouter.Modules.SocialListener;

public class BlueskySocialListener(ILogger logger) : SocialListener(logger)
{
  protected override Task<SocialInfo[]> PerformSearch(SocialTopicListeningTask task)
  {
    throw new NotImplementedException();
  }
}