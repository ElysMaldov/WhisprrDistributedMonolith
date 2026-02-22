using System.Net;
using System.Net.Http.Headers;
using Moq;
using Moq.Protected;
using Whisprr.BlueskyService.Models.Domain;
using Whisprr.BlueskyService.Modules.BlueskySessionStore;

namespace Whisprr.BlueskyService.Test.Modules.BlueskyAuthHandler;

using BlueskyAuthHandler = BlueskyService.Modules.BlueskyAuthHandler.BlueskyAuthHandler;

public class BlueskyAuthHandlerTest
{
  private readonly Mock<IBlueskySessionStore> _sessionStoreMock;
  private readonly Mock<HttpMessageHandler> _innerHandlerMock;

  public BlueskyAuthHandlerTest()
  {
    _sessionStoreMock = new Mock<IBlueskySessionStore>();
    _innerHandlerMock = new Mock<HttpMessageHandler>();
  }

  private HttpClient CreateClientWithHandler(BlueskyAuthHandler handler)
  {
    return new HttpClient(handler)
    {
      BaseAddress = new Uri("https://api.test.com")
    };
  }

  /// <summary>
  /// Test 1: When a session exists and no Authorization header is set,
  /// the handler should add the Bearer token from the session.
  /// </summary>
  [Fact]
  public async Task SendAsync_SessionExistsNoAuthHeader_AddsBearerToken()
  {
    // Arrange
    var accessToken = "test-access-token";
    var session = new BlueskySession
    {
      AccessToken = accessToken,
      RefreshToken = "refresh-token"
    };

    _sessionStoreMock
        .Setup(x => x.GetSessionAsync())
        .ReturnsAsync(session);

    HttpRequestMessage? capturedRequest = null;
    _innerHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

    var handler = new BlueskyAuthHandler(_sessionStoreMock.Object)
    {
      InnerHandler = _innerHandlerMock.Object
    };
    var client = CreateClientWithHandler(handler);

    // Act
    var response = await client.GetAsync("/test");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(capturedRequest);
    Assert.NotNull(capturedRequest!.Headers.Authorization);
    Assert.Equal("Bearer", capturedRequest.Headers.Authorization.Scheme);
    Assert.Equal(accessToken, capturedRequest.Headers.Authorization.Parameter);
  }

  /// <summary>
  /// Test 2: When a session exists but the request already has an Authorization header,
  /// the handler should NOT overwrite the existing header.
  /// </summary>
  [Fact]
  public async Task SendAsync_AuthHeaderAlreadySet_DoesNotOverwrite()
  {
    // Arrange
    var existingToken = "existing-token";
    var sessionToken = "session-token";
    var session = new BlueskySession
    {
      AccessToken = sessionToken,
      RefreshToken = "refresh-token"
    };

    _sessionStoreMock
        .Setup(x => x.GetSessionAsync())
        .ReturnsAsync(session);

    HttpRequestMessage? capturedRequest = null;
    _innerHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

    var handler = new BlueskyAuthHandler(_sessionStoreMock.Object)
    {
      InnerHandler = _innerHandlerMock.Object
    };
    var client = CreateClientWithHandler(handler);

    // Create a request with an existing Authorization header
    var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com/test");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", existingToken);

    // Act
    var response = await client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(capturedRequest);
    Assert.NotNull(capturedRequest!.Headers.Authorization);
    // The existing token should be preserved, not overwritten with session token
    Assert.Equal("Bearer", capturedRequest.Headers.Authorization.Scheme);
    Assert.Equal(existingToken, capturedRequest.Headers.Authorization.Parameter);
  }

  /// <summary>
  /// Test 3: When no session exists in the store,
  /// the handler should throw an InvalidOperationException.
  /// </summary>
  [Fact]
  public async Task SendAsync_NoSession_ThrowsInvalidOperationException()
  {
    // Arrange
    _sessionStoreMock
        .Setup(x => x.GetSessionAsync())
        .ReturnsAsync((BlueskySession?)null);

    var handler = new BlueskyAuthHandler(_sessionStoreMock.Object)
    {
      InnerHandler = _innerHandlerMock.Object
    };
    var client = CreateClientWithHandler(handler);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
        () => client.GetAsync("/test"));

    Assert.Contains("No session token found", exception.Message);

    // The inner handler should never be called
    _innerHandlerMock.Protected().Verify(
        "SendAsync",
        Times.Never(),
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>());
  }

  /// <summary>
  /// Test 4: Verifies that the session store is called exactly once per request.
  /// </summary>
  [Fact]
  public async Task SendAsync_CallsSessionStoreOnce()
  {
    // Arrange
    var session = new BlueskySession
    {
      AccessToken = "access-token",
      RefreshToken = "refresh-token"
    };

    _sessionStoreMock
        .Setup(x => x.GetSessionAsync())
        .ReturnsAsync(session);

    _innerHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

    var handler = new BlueskyAuthHandler(_sessionStoreMock.Object)
    {
      InnerHandler = _innerHandlerMock.Object
    };
    var client = CreateClientWithHandler(handler);

    // Act
    await client.GetAsync("/test");

    // Assert
    _sessionStoreMock.Verify(x => x.GetSessionAsync(), Times.Once);
  }
}
