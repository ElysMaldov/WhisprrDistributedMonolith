using Whisprr.BlueskyService.Modules.BlueskyService;
using Whisprr.Entities.Models;

namespace Whisprr.SocialScouter.Modules.SocialListener.Bluesky;

/// <summary>
/// Bluesky-specific implementation of the social listener.
/// Searches Bluesky posts using keywords from the listening task.
/// </summary>
public class BlueskySocialListener(
    ILogger<BlueskySocialListener> logger,
    IBlueskyService blueskyService) : SocialListener<BlueskySocialListener>(logger)
{
    protected override async Task<SocialInfo[]> PerformSearch(SocialTopicListeningTask task)
    {
        var query = task.Query;

        var blueskyPosts = (await blueskyService.SearchPosts(q: query)).Posts;
        SocialInfo[] mappedPosts = blueskyPosts.Select(p => p.ToSocialInfo()).ToArray();

        return mappedPosts;
    }
}
