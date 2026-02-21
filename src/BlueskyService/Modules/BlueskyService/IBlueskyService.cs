using System.Globalization;
using BlueskyService.Models.Dto;
using Whisprr.Bluesky.Enums;

namespace BlueskyService.Modules.BlueskyService;

public interface IBlueskyService
{
  public Task<SearchPostDto[]> SearchPosts(
    string q,
    PostSortOrder? sort,
    DateTimeOffset? since,
    DateTimeOffset? until,
    CultureInfo? lang,
    int? limit
  );
}