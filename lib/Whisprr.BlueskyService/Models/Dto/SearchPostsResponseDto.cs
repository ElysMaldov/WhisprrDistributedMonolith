using System.Text.Json;

namespace Whisprr.BlueskyService.Models.Dto;

public readonly struct SearchPostsResponseDto(
    string? cursor,
    int hitsTotal,
    PostViewDto[] posts)
{
    public string? Cursor { get; } = cursor;
    public int HitsTotal { get; } = hitsTotal;
    public PostViewDto[] Posts { get; } = posts;

    public static SearchPostsResponseDto FromJson(string json)
    {
        return JsonSerializer.Deserialize(json, BlueskyDtoContext.Default.SearchPostsResponseDto);
    }
}
