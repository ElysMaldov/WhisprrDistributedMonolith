using System.Globalization;
using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using Whisprr.BlueskyService.Enums;
using Whisprr.BlueskyService.Models.Dto;

// Alias to avoid namespace conflict with test class namespace
using BlueskyServiceClass = Whisprr.BlueskyService.Modules.BlueskyService.BlueskyService;

namespace Whisprr.BlueskyService.Test.Modules.BlueskyService;

public class BlueskyServiceTest
{
  private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

  public BlueskyServiceTest()
  {
    _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
  }

  private BlueskyServiceClass CreateService()
  {
    var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
    {
      BaseAddress = new Uri("https://api.bsky.app")
    };
    return new BlueskyServiceClass(httpClient);
  }

  private void SetupHttpResponse(HttpResponseMessage response)
  {
    _httpMessageHandlerMock
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync(response);
  }

  #region SearchPosts Tests

  /// <summary>
  /// Test 1: SearchPosts with query only should call the correct endpoint with query parameter.
  /// </summary>
  [Fact]
  public async Task SearchPosts_WithQueryOnly_CallsEndpointWithQueryParam()
  {
    // Arrange
    var query = "test query";
    HttpRequestMessage? capturedRequest = null;

    _httpMessageHandlerMock
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
      .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{ \"posts\": [] }")
      });

    var service = CreateService();

    // Act
    var result = await service.SearchPosts(query, null, null, null, null, null);

    // Assert
    Assert.NotNull(capturedRequest);
    Assert.Contains("/xrpc/app.bsky.feed.searchPosts", capturedRequest!.RequestUri!.ToString());
    Assert.Contains($"q={Uri.EscapeDataString(query)}", capturedRequest.RequestUri.Query);
  }

  /// <summary>
  /// Test 2: SearchPosts with sort order should include sort parameter.
  /// </summary>
  [Theory]
  [InlineData(PostSortOrder.Top, "top")]
  [InlineData(PostSortOrder.Latest, "latest")]
  public async Task SearchPosts_WithSortOrder_IncludesSortParam(PostSortOrder sort, string expectedSort)
  {
    // Arrange
    var query = "test";
    HttpRequestMessage? capturedRequest = null;

    _httpMessageHandlerMock
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
      .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{ \"posts\": [] }")
      });

    var service = CreateService();

    // Act
    await service.SearchPosts(query, sort, null, null, null, null);

    // Assert
    Assert.NotNull(capturedRequest);
    Assert.Contains($"sort={expectedSort}", capturedRequest!.RequestUri!.Query);
  }

  /// <summary>
  /// Test 3: SearchPosts with since date should include since parameter in ISO 8601 format.
  /// </summary>
  [Fact]
  public async Task SearchPosts_WithSinceDate_IncludesSinceParam()
  {
    // Arrange
    var query = "test";
    var since = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
    HttpRequestMessage? capturedRequest = null;

    _httpMessageHandlerMock
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
      .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{ \"posts\": [] }")
      });

    var service = CreateService();

    // Act
    await service.SearchPosts(query, null, since, null, null, null);

    // Assert
    Assert.NotNull(capturedRequest);
    Assert.Contains("since=2024-01-15T10%3A30%3A00", capturedRequest!.RequestUri!.Query);
  }

  /// <summary>
  /// Test 4: SearchPosts with until date should include until parameter in ISO 8601 format.
  /// </summary>
  [Fact]
  public async Task SearchPosts_WithUntilDate_IncludesUntilParam()
  {
    // Arrange
    var query = "test";
    var until = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);
    HttpRequestMessage? capturedRequest = null;

    _httpMessageHandlerMock
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
      .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{ \"posts\": [] }")
      });

    var service = CreateService();

    // Act
    await service.SearchPosts(query, null, null, until, null, null);

    // Assert
    Assert.NotNull(capturedRequest);
    Assert.Contains("until=2024-12-31T23%3A59%3A59", capturedRequest!.RequestUri!.Query);
  }

  /// <summary>
  /// Test 5: SearchPosts with language should include lang parameter.
  /// </summary>
  [Theory]
  [InlineData("en")]
  [InlineData("ja")]
  [InlineData("de")]
  public async Task SearchPosts_WithLanguage_IncludesLangParam(string langCode)
  {
    // Arrange
    var query = "test";
    var culture = new CultureInfo(langCode);
    HttpRequestMessage? capturedRequest = null;

    _httpMessageHandlerMock
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
      .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{ \"posts\": [] }")
      });

    var service = CreateService();

    // Act
    await service.SearchPosts(query, null, null, null, culture, null);

    // Assert
    Assert.NotNull(capturedRequest);
    Assert.Contains($"lang={langCode}", capturedRequest!.RequestUri!.Query);
  }

  /// <summary>
  /// Test 6: SearchPosts with limit should include limit parameter.
  /// </summary>
  [Theory]
  [InlineData(10)]
  [InlineData(50)]
  [InlineData(100)]
  public async Task SearchPosts_WithLimit_IncludesLimitParam(int limit)
  {
    // Arrange
    var query = "test";
    HttpRequestMessage? capturedRequest = null;

    _httpMessageHandlerMock
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
      .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{ \"posts\": [] }")
      });

    var service = CreateService();

    // Act
    await service.SearchPosts(query, null, null, null, null, limit);

    // Assert
    Assert.NotNull(capturedRequest);
    Assert.Contains($"limit={limit}", capturedRequest!.RequestUri!.Query);
  }

  /// <summary>
  /// Test 7: SearchPosts should return deserialized SearchPostsResponseDto.
  /// </summary>
  [Fact]
  public async Task SearchPosts_WithValidResponse_ReturnsSearchPostsResponseDto()
  {
    // Arrange
    var query = "test";
    var jsonResponse = @"{
      ""posts"": [
        {
          ""uri"": ""at://did:plc:abc123/app.bsky.feed.post/def456"",
          ""cid"": ""bafyrei..."",
          ""author"": {
            ""did"": ""did:plc:abc123"",
            ""handle"": ""testuser.bsky.social"",
            ""displayName"": ""Test User""
          },
          ""record"": {
            ""text"": ""This is a test post"",
            ""createdAt"": ""2024-01-15T10:30:00.000Z""
          },
          ""replyCount"": 5,
          ""repostCount"": 10,
          ""likeCount"": 25,
          ""indexedAt"": ""2024-01-15T10:30:00.000Z""
        }
      ]
    }";

    SetupHttpResponse(new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(jsonResponse)
    });

    var service = CreateService();

    // Act
    var result = await service.SearchPosts(query, null, null, null, null, null);

    // Assert
    Assert.IsType<SearchPostsResponseDto>(result);
    Assert.NotNull(result.Posts);
    Assert.Single(result.Posts);
  }

  /// <summary>
  /// Test 8: SearchPosts with empty response should return empty array.
  /// </summary>
  [Fact]
  public async Task SearchPosts_WithEmptyResponse_ReturnsEmptyArray()
  {
    // Arrange
    var query = "test";
    var jsonResponse = @"{ ""posts"": [] }";

    SetupHttpResponse(new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(jsonResponse)
    });

    var service = CreateService();

    // Act
    var result = await service.SearchPosts(query, null, null, null, null, null);

    // Assert
    Assert.NotNull(result.Posts);
    Assert.Empty(result.Posts);
  }

  /// <summary>
  /// Test 9: SearchPosts with API error should throw HttpRequestException.
  /// </summary>
  [Theory]
  [InlineData(HttpStatusCode.BadRequest)]
  [InlineData(HttpStatusCode.Unauthorized)]
  [InlineData(HttpStatusCode.TooManyRequests)]
  [InlineData(HttpStatusCode.InternalServerError)]
  public async Task SearchPosts_WithApiError_ThrowsHttpRequestException(HttpStatusCode statusCode)
  {
    // Arrange
    var query = "test";

    SetupHttpResponse(new HttpResponseMessage(statusCode));

    var service = CreateService();

    // Act & Assert
    await Assert.ThrowsAsync<HttpRequestException>(() =>
      service.SearchPosts(query, null, null, null, null, null));
  }

  /// <summary>
  /// Test 10: SearchPosts should use GET method.
  /// </summary>
  [Fact]
  public async Task SearchPosts_UsesGetMethod()
  {
    // Arrange
    var query = "test";
    HttpRequestMessage? capturedRequest = null;

    _httpMessageHandlerMock
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
      .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{ \"posts\": [] }")
      });

    var service = CreateService();

    // Act
    await service.SearchPosts(query, null, null, null, null, null);

    // Assert
    Assert.NotNull(capturedRequest);
    Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
  }

  /// <summary>
  /// Test 11: SearchPosts with all parameters should include all query parameters.
  /// </summary>
  [Fact]
  public async Task SearchPosts_WithAllParameters_IncludesAllQueryParams()
  {
    // Arrange
    var query = "test query";
    var sort = PostSortOrder.Latest;
    var since = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
    var until = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);
    var lang = new CultureInfo("en");
    var limit = 50;
    HttpRequestMessage? capturedRequest = null;

    _httpMessageHandlerMock
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
      .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{ \"posts\": [] }")
      });

    var service = CreateService();

    // Act
    await service.SearchPosts(query, sort, since, until, lang, limit);

    // Assert
    Assert.NotNull(capturedRequest);
    var queryString = capturedRequest!.RequestUri!.Query;
    Assert.Contains($"q={Uri.EscapeDataString(query)}", queryString);
    Assert.Contains("sort=latest", queryString);
    Assert.Contains("since=2024-01-01T00%3A00%3A00", queryString);
    Assert.Contains("until=2024-12-31T23%3A59%3A59", queryString);
    Assert.Contains("lang=en", queryString);
    Assert.Contains("limit=50", queryString);
  }

  /// <summary>
  /// Test 12: SearchPosts should properly URL-encode special characters in query.
  /// </summary>
  [Theory]
  [InlineData("hello world", "hello%20world")]
  [InlineData("test&query", "test%26query")]
  [InlineData("100% sure", "100%25%20sure")]
  public async Task SearchPosts_SpecialCharactersInQuery_ProperlyEncoded(string query, string expectedEncoded)
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;

    _httpMessageHandlerMock
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
      .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{ \"posts\": [] }")
      });

    var service = CreateService();

    // Act
    await service.SearchPosts(query, null, null, null, null, null);

    // Assert
    Assert.NotNull(capturedRequest);
    Assert.Contains($"q={expectedEncoded}", capturedRequest!.RequestUri!.Query);
  }

  #endregion
}
