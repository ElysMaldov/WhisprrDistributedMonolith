using BlueskyService.Models.Dto;
using System.Globalization;
using Whisprr.Bluesky.Enums;

namespace BlueskyService.Modules.BlueskyService;

/// <summary>
/// Auth tokens are injected using the auth delegating handler so our services
/// can just focus on the core business logic.
/// </summary>
/// <param name="httpClient"></param>
public class BlueskyService : IBlueskyService
{
  public Task<SearchPostDto[]> SearchPosts(
    string q,
    PostSortOrder? sort,
    DateTimeOffset? since,
    DateTimeOffset? until,
    CultureInfo? lang,
    int? limit
    )
  {
    var endpoint = "/xrpc/app.bsky.feed.searchPosts";

    throw new NotImplementedException();
  }
}