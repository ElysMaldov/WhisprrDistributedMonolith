using System.Net;
using System.Net.Http.Headers;
using Whisprr.BlueskyService.Modules.BlueskyAuthService;
using Whisprr.BlueskyService.Modules.BlueskySessionStore;

namespace Whisprr.BlueskyService.Modules.BlueskyAuthHandler;

public class BlueskyAuthHandler(
    IBlueskySessionStore sessionStore,
    IServiceProvider serviceProvider) : DelegatingHandler
{
  protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
  {
    var session = await sessionStore.GetSessionAsync() ?? throw new InvalidOperationException("No session token found!");

    // Attach auth token to header for each request
    if (request.Headers.Authorization == null)
    {
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
    }

    var response = await base.SendAsync(request, cancellationToken);

    // Only refresh token if we get 401
    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
      var blueskyService = (IBlueskyAuthService)serviceProvider.GetService(typeof(IBlueskyAuthService))!;

      try
      {
        var newSession = await blueskyService.RefreshSession(session.RefreshToken);

        await sessionStore.SaveSessionAsync(newSession);

        // Retry the original request with the fresh token
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newSession.AccessToken);
        return await base.SendAsync(request, cancellationToken);
      }
      catch
      {
        throw new UnauthorizedAccessException("Session expired and refresh failed");
      }
    }

    return response;
  }
}