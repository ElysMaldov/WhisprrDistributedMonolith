using Whisprr.BlueskyService.Modules.BlueskyService;
using Whisprr.Entities.Models;
using Whisprr.SocialScouter.Models.Extensions;

namespace Whisprr.SocialScouter.Modules.SocialListener;

public class BlueskySocialListener(
    ILogger<BlueskySocialListener> logger,
    IBlueskyService blueskyService) : SocialListener<BlueskySocialListener>(logger)
{
  protected override async Task<SocialInfo[]> PerformSearch(SocialTopicListeningTask task)
  {
    var query = string.Join(" ", task.SocialTopic.Keywords);

    var blueskyPosts = (await blueskyService.SearchPosts(q: query)).Posts;
    SocialInfo[] mappedPosts = blueskyPosts.Select(p => p.ToSocialInfo()).ToArray();

    return mappedPosts;

  }
}
