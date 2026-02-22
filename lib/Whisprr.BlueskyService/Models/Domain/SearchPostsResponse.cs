namespace Whisprr.BlueskyService.Models.Domain;

public readonly struct SearchPostsResponse(
    string? cursor,
    int hitsTotal,
    BlueskyPost[] posts)
{
    public string? Cursor { get; } = cursor;
    public int HitsTotal { get; } = hitsTotal;
    public BlueskyPost[] Posts { get; } = posts;
}
