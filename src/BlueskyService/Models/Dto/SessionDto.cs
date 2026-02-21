using System.Text.Json;
using System.Text.Json.Serialization;
using BlueskyService.Models.Domain;
namespace BlueskyService.Models.Dto;

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
    return JsonSerializer.Deserialize(json, BlueskyJsonContext.Default.SessionDto);
  }


}

// Use json serializer to generate code that serializes the response JSON to this struct
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false)]
[JsonSerializable(typeof(SessionDto))]
internal partial class BlueskyJsonContext : JsonSerializerContext
{

}