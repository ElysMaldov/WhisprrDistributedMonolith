using System.Net.Http.Headers;
using Whisprr.BlueskyService.Modules.BlueskySessionStore;

namespace Whisprr.BlueskyService.Modules.BlueskyAuthHandler;

public class BlueskyAuthHandler(IBlueskySessionStore sessionStore) : DelegatingHandler
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

    return response;
  }
}