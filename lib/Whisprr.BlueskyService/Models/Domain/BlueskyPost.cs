namespace Whisprr.BlueskyService.Models.Domain;

public readonly struct BlueskyPost(
    string uri,
    string cId,
    BlueskyAuthor author,
    BlueskyPostRecord record,
    int bookmarkCount,
    int replyCount,
    int repostCount,
    int likeCount,
    int quoteCount,
    DateTimeOffset indexedAt,
    string[] labels)
{
    public string Uri { get; } = uri;
    public string CId { get; } = cId;
    public BlueskyAuthor Author { get; } = author;
    public BlueskyPostRecord Record { get; } = record;
    public int BookmarkCount { get; } = bookmarkCount;
    public int ReplyCount { get; } = replyCount;
    public int RepostCount { get; } = repostCount;
    public int LikeCount { get; } = likeCount;
    public int QuoteCount { get; } = quoteCount;
    public DateTimeOffset IndexedAt { get; } = indexedAt;
    public string[] Labels { get; } = labels;
}