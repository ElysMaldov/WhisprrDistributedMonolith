using System.Net;
using System.Net.Http.Headers;
using Whisprr.BlueskyService.Modules.BlueskyAuthService;
using Whisprr.BlueskyService.Modules.BlueskySessionStore;

namespace Whisprr.BlueskyService.Modules.BlueskyAuthHandler;

public class BlueskyRefreshSessionHandler(
    IBlueskySessionStore sessionStore,
    IServiceProvider serviceProvider) : DelegatingHandler
{
  protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
  {
    var response = await base.SendAsync(request, cancellationToken);

    // Only refresh token if we get 401
    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
      // Get this service through service provider since IBlueskyAuthService will use
      // HttpClient too to avoid circular dependency
      var blueskyService = (IBlueskyAuthService)serviceProvider.GetService(typeof(IBlueskyAuthService))!;
      var session = await sessionStore.GetSessionAsync() ?? throw new UnauthorizedAccessException("No refresh token found!");

      try
      {
        var newSession = await blueskyService.RefreshSession(session.RefreshToken);
        await sessionStore.SaveSessionAsync(newSession);

        // We update the header so the retried request works
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newSession.AccessToken);

        // Retry request
        return await base.SendAsync(request, cancellationToken);
      }
      catch
      {
        throw new UnauthorizedAccessException("The refresh failed. The session is officially canceled.");
      }
    }

    return response;
  }
}