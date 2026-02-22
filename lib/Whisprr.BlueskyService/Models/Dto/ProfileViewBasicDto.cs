using System.Text.Json;
using System.Text.Json.Serialization;
using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Models.Dto;

internal readonly struct ProfileViewBasicDto
{
    [JsonPropertyName("did")]
    public string DId { get; init; }

    [JsonPropertyName("handle")]
    public string Handle { get; init; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; }

    [JsonPropertyName("avatar")]
    public string Avatar { get; init; }

    [JsonPropertyName("labels")]
    public string[] Labels { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    public static ProfileViewBasicDto FromJson(string json)
    {
        return JsonSerializer.Deserialize(json, BlueskyDtoContext.Default.ProfileViewBasicDto);
    }

    public BlueskyAuthor ToDomain()
    {
        return new BlueskyAuthor(DId, Handle, DisplayName, Avatar, Labels, CreatedAt);
    }
}
