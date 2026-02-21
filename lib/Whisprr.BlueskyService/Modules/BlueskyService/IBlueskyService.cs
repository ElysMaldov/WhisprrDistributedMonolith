using System.Globalization;
using Whisprr.BlueskyService.Models.Dto;
using Whisprr.Bluesky.Enums;

namespace Whisprr.BlueskyService.Modules.BlueskyService;

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