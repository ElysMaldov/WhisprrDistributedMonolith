using System.Globalization;
using Whisprr.BlueskyService.Models.Dto;
using Whisprr.BlueskyService.Enums;

namespace Whisprr.BlueskyService.Modules.BlueskyService;

public interface IBlueskyService
{
  public Task<SearchPostsResponseDto> SearchPosts(
    string q,
    PostSortOrder? sort,
    DateTimeOffset? since,
    DateTimeOffset? until,
    CultureInfo? lang,
    int? limit
  );
}