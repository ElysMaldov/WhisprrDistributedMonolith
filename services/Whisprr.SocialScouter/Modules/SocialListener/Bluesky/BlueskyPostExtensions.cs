using Whisprr.BlueskyService.Models.Domain;
using Whisprr.Entities.Models;

namespace Whisprr.SocialScouter.Modules.SocialListener.Bluesky;

/// <summary>
/// Extension methods for converting Bluesky domain models to SocialInfo entities.
/// </summary>
public static class BlueskyPostExtensions
{
    /// <summary>
    /// Converts a BlueskyPost to a SocialInfo entity.
    /// </summary>
    public static SocialInfo ToSocialInfo(this BlueskyPost blueskyPost)
    {
        return new SocialInfo()
        {
            OriginalUrl = blueskyPost.Uri,
            Content = blueskyPost.Record.Text,
            OriginalId = blueskyPost.CId,
        };
    }
}
