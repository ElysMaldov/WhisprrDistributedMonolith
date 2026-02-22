using Whisprr.BlueskyService.Models.Domain;
using Whisprr.BlueskyService.Models.Dto;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace Whisprr.BlueskyService.Modules.BlueskyAuthService;

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

  private readonly SemaphoreSlim _refreshSemaphore = new(1, 1); // Start 1, max 1
  private BlueskySession? _cachedRefreshedSession;

  /// <summary>
  /// Uses semaphore for multithreading safety to limit if multiple
  /// request gets 401 so we only need to refresh once, and the other failed
  /// requests can instantly retry using the latest refresh tokens.
  ///
  /// Multithread safety is required here since we may get multiple concurrent request
  /// where each request is handled by a different thread which activate this method
  /// from the delegating handler multiple times.
  ///
  /// I don't put the semaphore logic into the <see cref="BlueskyRefreshSessionHandler"/> since
  /// that will be a transient dependency that might be recreated for different request,
  /// rendering the semaphore logic useless since each instance willl have their own
  /// semaphores. By putting the logic in this singleton service, we avoid that.
  ///
  /// Example: if 4 requests fails 401, the first request will take the semaphore,
  /// while the other 3 will wait for it to go back. The first request will
  /// get the new refresh token, cache it, return the semaphore, the other request
  /// continue the request, sees there is already a cached refresh session, and instantly
  /// use the cached one instead of making 3 requests again
  /// </summary>
  /// <param name="refreshToken"></param>
  /// <returns></returns>
  public async Task<BlueskySession> RefreshSession(string refreshToken)
  {
    await _refreshSemaphore.WaitAsync();

    try
    {
      // If the 'refreshToken' we just received is DIFFERENT from the one currently in our
      // system/store, it means another thread has already refreshed it.
      if (_cachedRefreshedSession.HasValue && _cachedRefreshedSession.Value.RefreshToken != refreshToken)
      {
        return _cachedRefreshedSession.Value;
      }

      var endpoint = "/xrpc/com.atproto.server.refreshSession";

      // We need to manually create a seperate request to override the auth header only for this request
      using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);

      using HttpResponseMessage response = await httpClient.SendAsync(request);

      response.EnsureSuccessStatusCode();

      var jsonResponse = await response.Content.ReadAsStringAsync();
      var sessionDto = SessionDto.FromJson(jsonResponse);
      var session = sessionDto.ToDomain();
      _cachedRefreshedSession = session;

      return session;
    }
    finally
    {
      _refreshSemaphore.Release();
    }
  }
}