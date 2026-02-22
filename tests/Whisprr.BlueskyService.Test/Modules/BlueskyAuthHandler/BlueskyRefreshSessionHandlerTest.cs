using System.Net;
using Moq;
using Moq.Protected;
using Whisprr.BlueskyService.Models.Domain;
using Whisprr.BlueskyService.Modules.BlueskyAuthHandler;
using Whisprr.BlueskyService.Modules.BlueskyAuthService;
using Whisprr.BlueskyService.Modules.BlueskySessionStore;

namespace Whisprr.BlueskyService.Test.Modules.BlueskyAuthHandler;

public class BlueskyRefreshSessionHandlerTest
{
  private readonly Mock<IBlueskySessionStore> _sessionStoreMock;
  private readonly Mock<IBlueskyAuthService> _authServiceMock;
  private readonly Mock<IServiceProvider> _serviceProviderMock;
  private readonly Mock<HttpMessageHandler> _innerHandlerMock;

  public BlueskyRefreshSessionHandlerTest()
  {
    _sessionStoreMock = new Mock<IBlueskySessionStore>();
    _authServiceMock = new Mock<IBlueskyAuthService>();
    _serviceProviderMock = new Mock<IServiceProvider>();
    _innerHandlerMock = new Mock<HttpMessageHandler>();

    _serviceProviderMock
        .Setup(sp => sp.GetService(typeof(IBlueskyAuthService)))
        .Returns(_authServiceMock.Object);
  }

  private HttpClient CreateClientWithHandler(BlueskyRefreshSessionHandler handler)
  {
    return new HttpClient(handler)
    {
      BaseAddress = new Uri("https://api.test.com")
    };
  }

  /// <summary>
  /// Test 1: Handler should only activate when getting a 401 Unauthorized error.
  /// For other status codes (like 200 OK), it should just return the response
  /// without attempting to refresh the token.
  /// </summary>
  [Theory]
  [InlineData(HttpStatusCode.OK)]
  [InlineData(HttpStatusCode.BadRequest)]
  [InlineData(HttpStatusCode.NotFound)]
  [InlineData(HttpStatusCode.InternalServerError)]
  public async Task SendAsync_Non401Response_DoesNotRefreshToken(HttpStatusCode statusCode)
  {
    // Arrange
    var handler = new BlueskyRefreshSessionHandler(
        _sessionStoreMock.Object,
        _serviceProviderMock.Object)
    {
      InnerHandler = _innerHandlerMock.Object
    };
    var client = CreateClientWithHandler(handler);

    var expectedResponse = new HttpResponseMessage(statusCode);

    _innerHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(expectedResponse);

    // Act
    var response = await client.GetAsync("/test");

    // Assert
    Assert.Equal(statusCode, response.StatusCode);
    _authServiceMock.Verify(
        x => x.RefreshSession(It.IsAny<string>()),
        Times.Never);
    _sessionStoreMock.Verify(
        x => x.GetSessionAsync(),
        Times.Never);
  }

  /// <summary>
  /// Test 2: When getting a 401, the handler should refresh the token and retry
  /// the original request with the new access token.
  /// </summary>
  [Fact]
  public async Task SendAsync_401Response_RefreshesTokenAndRetriesRequest()
  {
    // Arrange
    var handler = new BlueskyRefreshSessionHandler(
        _sessionStoreMock.Object,
        _serviceProviderMock.Object)
    {
      InnerHandler = _innerHandlerMock.Object
    };
    var client = CreateClientWithHandler(handler);

    var refreshToken = "refresh-token";
    var newAccessToken = "new-access-token";
    var session = new BlueskySession
    {
      AccessToken = "original-access-token",
      RefreshToken = refreshToken
    };
    var newSession = new BlueskySession
    {
      AccessToken = newAccessToken,
      RefreshToken = "new-refresh-token"
    };

    // First call returns 401, second call (retry) returns 200
    var callCount = 0;
    _innerHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(() =>
        {
          callCount++;
          return callCount == 1
              ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
              : new HttpResponseMessage(HttpStatusCode.OK);
        });

    _sessionStoreMock
        .Setup(x => x.GetSessionAsync())
        .ReturnsAsync(session);

    _authServiceMock
        .Setup(x => x.RefreshSession(refreshToken))
        .ReturnsAsync(newSession);

    // Act
    var response = await client.GetAsync("/test");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal(2, callCount); // Original + retry

    // Verify the token was refreshed
    _authServiceMock.Verify(x => x.RefreshSession(refreshToken), Times.Once);
    _sessionStoreMock.Verify(x => x.SaveSessionAsync(newSession), Times.Once);

    // Verify the request was retried with the new token
    _innerHandlerMock.Protected().Verify(
        "SendAsync",
        Times.Exactly(2),
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>());
  }

  /// <summary>
  /// Test 3: When 10 concurrent requests get 401, only the first one should
  /// refresh the token. The rest should use the cached token and retry.
  /// This tests the semaphore logic in BlueskyAuthService.
  /// </summary>
  [Fact]
  public async Task SendAsync_Concurrent401Requests_OnlyOneRefreshesToken()
  {
    // Arrange
    const int concurrentRequests = 10;
    var refreshToken = "refresh-token";
    var newAccessToken = "new-access-token";
    var session = new BlueskySession
    {
      AccessToken = "original-access-token",
      RefreshToken = refreshToken
    };
    var newSession = new BlueskySession
    {
      AccessToken = newAccessToken,
      RefreshToken = "new-refresh-token"
    };

    // Use a real BlueskyAuthService with a mock HttpClient to test the semaphore
    var authHandlerMock = new Mock<HttpMessageHandler>();
    authHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent("""{"accessJwt":"new-access-token","refreshJwt":"new-refresh-token","handle":"test.handle","did":"did:plc:test"}""")
        });

    var authHttpClient = new HttpClient(authHandlerMock.Object)
    {
      BaseAddress = new Uri("https://api.test.com")
    };

    var realAuthService = new BlueskyAuthService(authHttpClient);

    // Replace the mock with the real service that has semaphore
    var serviceProviderMock = new Mock<IServiceProvider>();
    serviceProviderMock
        .Setup(sp => sp.GetService(typeof(IBlueskyAuthService)))
        .Returns(realAuthService);

    // Setup session store
    var sessionStoreMock = new Mock<IBlueskySessionStore>();
    sessionStoreMock
        .Setup(x => x.GetSessionAsync())
        .ReturnsAsync(session);

    // Setup inner handler to return 401 first, then 200 on retry
    var innerHandlerMock = new Mock<HttpMessageHandler>();
    var requestCallCounts = new Dictionary<string, int>();
    innerHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
        {
          // Use the request URI as a key to track calls
          var key = req.RequestUri?.ToString() ?? "";
          lock (requestCallCounts)
          {
            if (!requestCallCounts.ContainsKey(key))
            {
              requestCallCounts[key] = 1;
              return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
            requestCallCounts[key]++;
            return new HttpResponseMessage(HttpStatusCode.OK);
          }
        });

    var handler = new BlueskyRefreshSessionHandler(
        sessionStoreMock.Object,
        serviceProviderMock.Object)
    {
      InnerHandler = innerHandlerMock.Object
    };
    var client = new HttpClient(handler)
    {
      BaseAddress = new Uri("https://api.test.com")
    };

    // Act
    var tasks = Enumerable.Range(0, concurrentRequests)
        .Select(i => client.GetAsync($"/test/{i}"))
        .ToArray();
    var responses = await Task.WhenAll(tasks);

    // Assert
    // All requests should succeed
    Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

    // The auth endpoint should only be called once due to semaphore
    authHandlerMock.Protected().Verify(
        "SendAsync",
        Times.Once(),
        ItExpr.Is<HttpRequestMessage>(req =>
            req.RequestUri!.ToString().Contains("/xrpc/com.atproto.server.refreshSession")),
        ItExpr.IsAny<CancellationToken>());

    // Each request should be sent twice (original 401 + retry with new token)
    innerHandlerMock.Protected().Verify(
        "SendAsync",
        Times.Exactly(concurrentRequests * 2),
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>());
  }

  /// <summary>
  /// Test that when no session exists in the store, an exception is thrown.
  /// </summary>
  [Fact]
  public async Task SendAsync_401WithNoSession_ThrowsUnauthorizedAccessException()
  {
    // Arrange
    var handler = new BlueskyRefreshSessionHandler(
        _sessionStoreMock.Object,
        _serviceProviderMock.Object)
    {
      InnerHandler = _innerHandlerMock.Object
    };
    var client = CreateClientWithHandler(handler);

    _innerHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

    _sessionStoreMock
        .Setup(x => x.GetSessionAsync())
        .ReturnsAsync((BlueskySession?)null);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
        () => client.GetAsync("/test"));

    Assert.Contains("No refresh token found", exception.Message);
  }

  /// <summary>
  /// Test that when refresh fails, an UnauthorizedAccessException is thrown.
  /// </summary>
  [Fact]
  public async Task SendAsync_RefreshFails_ThrowsUnauthorizedAccessException()
  {
    // Arrange
    var handler = new BlueskyRefreshSessionHandler(
        _sessionStoreMock.Object,
        _serviceProviderMock.Object)
    {
      InnerHandler = _innerHandlerMock.Object
    };
    var client = CreateClientWithHandler(handler);

    var session = new BlueskySession
    {
      AccessToken = "access-token",
      RefreshToken = "refresh-token"
    };

    _innerHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

    _sessionStoreMock
        .Setup(x => x.GetSessionAsync())
        .ReturnsAsync(session);

    _authServiceMock
        .Setup(x => x.RefreshSession(It.IsAny<string>()))
        .ThrowsAsync(new HttpRequestException("Refresh failed"));

    // Act & Assert
    var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
        () => client.GetAsync("/test"));

    Assert.Contains("The refresh failed", exception.Message);
  }
}
