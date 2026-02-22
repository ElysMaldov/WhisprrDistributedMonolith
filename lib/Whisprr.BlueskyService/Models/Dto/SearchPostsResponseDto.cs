using System.Text.Json;
using System.Text.Json.Serialization;
using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Models.Dto;

internal readonly struct SearchPostsResponseDto
{
    [JsonPropertyName("cursor")]
    public string? Cursor { get; init; }

    [JsonPropertyName("hitsTotal")]
    public int HitsTotal { get; init; }

    [JsonPropertyName("posts")]
    public PostViewDto[] Posts { get; init; }

    public static SearchPostsResponseDto FromJson(string json)
    {
        return JsonSerializer.Deserialize(json, BlueskyDtoContext.Default.SearchPostsResponseDto);
    }

    public SearchPostsResponse ToDomain()
    {
        return new SearchPostsResponse(
            Cursor,
            HitsTotal,
            Posts.Select(p => p.ToDomain()).ToArray()
        );
    }
}
