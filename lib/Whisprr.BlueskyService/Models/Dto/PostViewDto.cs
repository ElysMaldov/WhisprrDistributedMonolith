using System.Text.Json;
using System.Text.Json.Serialization;
using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Models.Dto;

internal readonly struct PostViewDto
{
    [JsonPropertyName("uri")]
    public string Uri { get; init; }

    [JsonPropertyName("cid")]
    public string CId { get; init; }

    [JsonPropertyName("author")]
    public ProfileViewBasicDto Author { get; init; }

    [JsonPropertyName("record")]
    public BlueskyPostRecordDto Record { get; init; }

    [JsonPropertyName("bookmarkCount")]
    public int BookmarkCount { get; init; }

    [JsonPropertyName("replyCount")]
    public int ReplyCount { get; init; }

    [JsonPropertyName("repostCount")]
    public int RepostCount { get; init; }

    [JsonPropertyName("likeCount")]
    public int LikeCount { get; init; }

    [JsonPropertyName("quoteCount")]
    public int QuoteCount { get; init; }

    [JsonPropertyName("indexedAt")]
    public DateTimeOffset IndexedAt { get; init; }

    [JsonPropertyName("labels")]
    public string[] Labels { get; init; }

    public static PostViewDto FromJson(string json)
    {
        return JsonSerializer.Deserialize(json, BlueskyDtoContext.Default.PostViewDto);
    }

    public BlueskyPost ToDomain()
    {
        return new BlueskyPost(
            Uri,
            CId,
            Author.ToDomain(),
            Record.ToDomain(),
            BookmarkCount,
            ReplyCount,
            RepostCount,
            LikeCount,
            QuoteCount,
            IndexedAt,
            Labels);
    }
}
