using Whisprr.BlueskyService.Models.Dto;
using Whisprr.BlueskyService.Models.Domain;
using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using Whisprr.BlueskyService.Enums;

namespace Whisprr.BlueskyService.Modules.BlueskyService;

/// <summary>
/// Auth tokens are injected using the auth delegating handler so our services
/// can just focus on the core business logic.
/// </summary>
/// <param name="httpClient"></param>
public class BlueskyService(HttpClient httpClient) : IBlueskyService
{
  public async Task<SearchPostsResponse> SearchPosts(
    string q,
    PostSortOrder? sort = null,
    DateTimeOffset? since = null,
    DateTimeOffset? until = null,
    CultureInfo? lang = null,
    int? limit = null
    )
  {
    var endpoint = "/xrpc/app.bsky.feed.searchPosts";

    Dictionary<string, string?> query = new()
    {
      ["q"] = q,
      ["sort"] = sort?.ToApiString(),
      ["since"] = since?.ToString("o"), // Convert to ISO 8601
      ["until"] = until?.ToString("o"),
      ["lang"] = lang?.TwoLetterISOLanguageName,
      ["limit"] = limit?.ToString()
    };

    var uri = QueryHelpers.AddQueryString(endpoint, query);

    var response = await httpClient.GetAsync(uri);
    response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadAsStringAsync();
    var dto = SearchPostsResponseDto.FromJson(json);

    return dto.ToDomain();
  }
}