using System.Text.Json;
using System.Text.Json.Serialization;
using Whisprr.BlueskyService.Models.Domain;
namespace Whisprr.BlueskyService.Models.Dto;

public readonly struct SessionDto(string accessJwt, string refreshJwt)
{
  public string AccessJwt { get; } = accessJwt;
  public string RefreshJwt { get; } = refreshJwt;

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

