using BlueskyService.Models.Domain;
using BlueskyService.Models.Dto;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace BlueskyService.Modules.BlueskyAuthService;

public class BlueskyAuthService(HttpClient httpClient) : IBlueskyAuthService
{
  public async Task<BlueskySession> CreateSession(string bskyHandle, string bskyPassword)
  {
    var endpoint = "/xrpc/com.atproto.server.createSession";

    // Using is used to dispose these resources once done
    using StringContent jsonContent = new(
        JsonSerializer.Serialize(new
        {
          identifier = bskyHandle,
          password = bskyPassword
        }),
        Encoding.UTF8,
        "application/json");

    using HttpResponseMessage response = await httpClient.PostAsync(
        endpoint,
        jsonContent);

    response.EnsureSuccessStatusCode();

    var jsonResponse = await response.Content.ReadAsStringAsync();
    var sessionDto = SessionDto.FromJson(jsonResponse);
    var session = sessionDto.ToDomain();

    return session;
  }

  public async Task<BlueskySession> RefreshSession(string refreshToken)
  {
    var endpoint = "/xrpc/com.atproto.server.refreshSession";

    // We need to manually create a seperate request to override the auth header only for this request
    using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);

    using HttpResponseMessage response = await httpClient.SendAsync(request);

    response.EnsureSuccessStatusCode();

    var jsonResponse = await response.Content.ReadAsStringAsync();
    var sessionDto = SessionDto.FromJson(jsonResponse);
    var session = sessionDto.ToDomain();

    return session;
  }
}