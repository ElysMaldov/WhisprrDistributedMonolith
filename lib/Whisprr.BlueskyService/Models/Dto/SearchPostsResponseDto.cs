using System.Text.Json;
using System.Text.Json.Serialization;

namespace Whisprr.BlueskyService.Models.Dto;

public readonly struct SearchPostsResponseDto
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
}
