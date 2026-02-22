using Whisprr.BlueskyService.Models.Domain;
using Whisprr.Entities.Models;

namespace Whisprr.SocialScouter.Models.Extensions;

public static class BlueskyPostExtensions
{
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