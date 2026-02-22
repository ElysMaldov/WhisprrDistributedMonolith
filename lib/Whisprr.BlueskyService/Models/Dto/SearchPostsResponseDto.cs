using System.Text.Json;

namespace Whisprr.BlueskyService.Models.Dto;

public readonly struct SearchPostsResponseDto(
    string? cursor,
    int hitsTotal,
    SearchPostDto[] posts)
{
    public string? Cursor { get; } = cursor;
    public int HitsTotal { get; } = hitsTotal;
    public SearchPostDto[] Posts { get; } = posts;

    public static SearchPostsResponseDto FromJson(string json)
    {
        return JsonSerializer.Deserialize(json, BlueskyDtoContext.Default.SearchPostsResponseDto);
    }
}
