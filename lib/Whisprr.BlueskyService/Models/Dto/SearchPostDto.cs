using System.Text.Json;
using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Models.Dto;

public readonly struct SearchPostDto(
    string uri,
    string cId,
    BlueskyAuthorDto author,
    BlueskyPostRecordDto record,
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
  public BlueskyAuthorDto Author { get; } = author;
  public BlueskyPostRecordDto Record { get; } = record;
  public int BookmarkCount { get; } = bookmarkCount;
  public int ReplyCount { get; } = replyCount;
  public int RepostCount { get; } = repostCount;
  public int LikeCount { get; } = likeCount;
  public int QuoteCount { get; } = quoteCount;
  public DateTimeOffset IndexedAt { get; } = indexedAt;
  public string[] Labels { get; } = labels;

  public static SearchPostDto FromJson(string json)
  {
    return JsonSerializer.Deserialize(json, BlueskyDtoContext.Default.SearchPostDto);
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
