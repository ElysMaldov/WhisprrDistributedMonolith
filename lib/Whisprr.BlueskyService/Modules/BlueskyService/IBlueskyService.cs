using System.Globalization;
using Whisprr.BlueskyService.Models.Domain;
using Whisprr.BlueskyService.Enums;

namespace Whisprr.BlueskyService.Modules.BlueskyService;

public interface IBlueskyService
{
  public Task<SearchPostsResponse> SearchPosts(
    string q,
    PostSortOrder? sort = null,
    DateTimeOffset? since = null,
    DateTimeOffset? until = null,
    CultureInfo? lang = null,
    int? limit = null
  );
}