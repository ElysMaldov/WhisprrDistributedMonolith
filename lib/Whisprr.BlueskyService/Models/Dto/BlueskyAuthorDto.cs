using System.Text.Json;
using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Models.Dto;

public readonly struct BlueskyAuthorDto(
    string dId,
    string handle,
    string displayName,
    string avatar,
    string[] labels,
    DateTimeOffset createdAt)
{
  public string DId { get; } = dId;
  public string Handle { get; } = handle;
  public string DisplayName { get; } = displayName;
  public string Avatar { get; } = avatar;
  public string[] Labels { get; } = labels;
  public DateTimeOffset CreatedAt { get; } = createdAt;

  public static BlueskyAuthorDto FromJson(string json)
  {
    return JsonSerializer.Deserialize(json, BlueskyDtoContext.Default.BlueskyAuthorDto);
  }

  public BlueskyAuthor ToDomain()
  {
    return new BlueskyAuthor(DId, Handle, DisplayName, Avatar, Labels, CreatedAt);
  }
}