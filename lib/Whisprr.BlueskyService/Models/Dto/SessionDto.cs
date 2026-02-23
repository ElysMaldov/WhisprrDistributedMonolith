using System.Text.Json;
using System.Text.Json.Serialization;
using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Models.Dto;

internal readonly struct SessionDto
{
    [JsonPropertyName("accessJwt")]
    public string AccessJwt { get; init; }

    [JsonPropertyName("refreshJwt")]
    public string RefreshJwt { get; init; }

    public BlueskySession ToDomain()
    {
        return new BlueskySession
        {
            AccessToken = AccessJwt,
            RefreshToken = RefreshJwt
        };
    }

    public static SessionDto FromJson(string json)
    {
        return JsonSerializer.Deserialize(json, BlueskyDtoContext.Default.SessionDto);
    }
}
