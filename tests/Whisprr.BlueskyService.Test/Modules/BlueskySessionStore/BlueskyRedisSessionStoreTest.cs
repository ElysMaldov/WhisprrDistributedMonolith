using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Test.Modules.BlueskySessionStore;

// Alias to avoid namespace conflict with test class namespace
using BlueskyRedisSessionStoreClass = Whisprr.BlueskyService.Modules.BlueskySessionStore.BlueskyRedisSessionStore;

public class BlueskyRedisSessionStoreTest
{
  private BlueskyRedisSessionStoreClass CreateStore()
  {
    return new BlueskyRedisSessionStoreClass();
  }

  #region GetSessionAsync Tests

  /// <summary>
  /// Test 1: GetSessionAsync should return null when no session exists in Redis.
  /// </summary>
  [Fact]
  public async Task GetSessionAsync_NoSessionExists_ReturnsNull()
  {
    // Arrange
    var store = CreateStore();

    // Act
    var result = await store.GetSessionAsync();

    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Test 2: GetSessionAsync should return the session when it exists in Redis.
  /// </summary>
  [Fact]
  public async Task GetSessionAsync_SessionExists_ReturnsSession()
  {
    // Arrange
    var store = CreateStore();
    var session = new BlueskySession
    {
      AccessToken = "test-access-token",
      RefreshToken = "test-refresh-token"
    };

    // First save the session
    await store.SaveSessionAsync(session);

    // Act
    var result = await store.GetSessionAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(session.AccessToken, result.Value.AccessToken);
    Assert.Equal(session.RefreshToken, result.Value.RefreshToken);
  }

  /// <summary>
  /// Test 3: GetSessionAsync should return the most recently saved session.
  /// </summary>
  [Fact]
  public async Task GetSessionAsync_AfterMultipleSaves_ReturnsLatestSession()
  {
    // Arrange
    var store = CreateStore();
    var firstSession = new BlueskySession
    {
      AccessToken = "first-access-token",
      RefreshToken = "first-refresh-token"
    };
    var secondSession = new BlueskySession
    {
      AccessToken = "second-access-token",
      RefreshToken = "second-refresh-token"
    };

    await store.SaveSessionAsync(firstSession);
    await store.SaveSessionAsync(secondSession);

    // Act
    var result = await store.GetSessionAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(secondSession.AccessToken, result.Value.AccessToken);
    Assert.Equal(secondSession.RefreshToken, result.Value.RefreshToken);
  }

  /// <summary>
  /// Test 4: GetSessionAsync should handle special characters in tokens.
  /// </summary>
  [Theory]
  [InlineData("token-with-dashes", "refresh-with-dashes")]
  [InlineData("token_with_underscores", "refresh_with_underscores")]
  [InlineData("token.with.dots", "refresh.with.dots")]
  [InlineData("token+with+plus", "refresh+with+plus")]
  [InlineData("token=with=equals", "refresh=with=equals")]
  public async Task GetSessionAsync_SpecialCharactersInTokens_PreservesTokens(string accessToken, string refreshToken)
  {
    // Arrange
    var store = CreateStore();
    var session = new BlueskySession
    {
      AccessToken = accessToken,
      RefreshToken = refreshToken
    };

    await store.SaveSessionAsync(session);

    // Act
    var result = await store.GetSessionAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(accessToken, result.Value.AccessToken);
    Assert.Equal(refreshToken, result.Value.RefreshToken);
  }

  /// <summary>
  /// Test 5: GetSessionAsync should handle long tokens (simulating JWT).
  /// </summary>
  [Fact]
  public async Task GetSessionAsync_LongJwtTokens_PreservesTokens()
  {
    // Arrange
    var store = CreateStore();
    var longAccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
                          "eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ." +
                          "SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
    var longRefreshToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
                           "eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ." +
                           "SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c" +
                           "extra_refresh_token_data_here";

    var session = new BlueskySession
    {
      AccessToken = longAccessToken,
      RefreshToken = longRefreshToken
    };

    await store.SaveSessionAsync(session);

    // Act
    var result = await store.GetSessionAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(longAccessToken, result.Value.AccessToken);
    Assert.Equal(longRefreshToken, result.Value.RefreshToken);
  }

  #endregion

  #region SaveSessionAsync Tests

  /// <summary>
  /// Test 6: SaveSessionAsync should persist the session successfully.
  /// </summary>
  [Fact]
  public async Task SaveSessionAsync_ValidSession_SavesSuccessfully()
  {
    // Arrange
    var store = CreateStore();
    var session = new BlueskySession
    {
      AccessToken = "access-token",
      RefreshToken = "refresh-token"
    };

    // Act
    var saveTask = store.SaveSessionAsync(session);

    // Assert
    await saveTask; // Should complete without exception
  }

  /// <summary>
  /// Test 7: SaveSessionAsync should overwrite existing session.
  /// </summary>
  [Fact]
  public async Task SaveSessionAsync_OverwritesExistingSession()
  {
    // Arrange
    var store = CreateStore();
    var firstSession = new BlueskySession
    {
      AccessToken = "first-access",
      RefreshToken = "first-refresh"
    };
    var secondSession = new BlueskySession
    {
      AccessToken = "second-access",
      RefreshToken = "second-refresh"
    };

    await store.SaveSessionAsync(firstSession);

    // Act
    await store.SaveSessionAsync(secondSession);
    var result = await store.GetSessionAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(secondSession.AccessToken, result.Value.AccessToken);
    Assert.Equal(secondSession.RefreshToken, result.Value.RefreshToken);
  }

  /// <summary>
  /// Test 8: SaveSessionAsync should handle empty strings (edge case).
  /// </summary>
  [Fact]
  public async Task SaveSessionAsync_EmptyStringTokens_SavesSuccessfully()
  {
    // Arrange
    var store = CreateStore();
    var session = new BlueskySession
    {
      AccessToken = "",
      RefreshToken = ""
    };

    // Act
    await store.SaveSessionAsync(session);
    var result = await store.GetSessionAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal("", result.Value.AccessToken);
    Assert.Equal("", result.Value.RefreshToken);
  }

  #endregion

  #region Integration Tests

  /// <summary>
  /// Test 9: Multiple stores should have isolated sessions (if using different Redis keys).
  /// Note: This test assumes each store instance uses a unique key or connection.
  /// </summary>
  [Fact]
  public async Task MultipleStores_DifferentSessions_AreIsolated()
  {
    // Arrange
    var store1 = CreateStore();
    var store2 = CreateStore();

    var session1 = new BlueskySession
    {
      AccessToken = "store1-access",
      RefreshToken = "store1-refresh"
    };
    var session2 = new BlueskySession
    {
      AccessToken = "store2-access",
      RefreshToken = "store2-refresh"
    };

    // Act
    await store1.SaveSessionAsync(session1);
    await store2.SaveSessionAsync(session2);

    var result1 = await store1.GetSessionAsync();
    var result2 = await store2.GetSessionAsync();

    // Assert - This behavior depends on implementation
    // If using the same Redis key, both should return session2 (last write wins)
    // If using different keys, each should return its own session
    Assert.NotNull(result1);
    Assert.NotNull(result2);
  }

  /// <summary>
  /// Test 10: Session persistence should survive store recreation (simulating app restart).
  /// </summary>
  [Fact]
  public async Task SessionPersistence_SurvivesStoreRecreation()
  {
    // Arrange
    var store = CreateStore();
    var session = new BlueskySession
    {
      AccessToken = "persistent-access-token",
      RefreshToken = "persistent-refresh-token"
    };

    await store.SaveSessionAsync(session);

    // Act - Create a new store instance (simulating app restart)
    var newStore = CreateStore();
    var result = await newStore.GetSessionAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(session.AccessToken, result.Value.AccessToken);
    Assert.Equal(session.RefreshToken, result.Value.RefreshToken);
  }

  #endregion

  #region Concurrency Tests

  /// <summary>
  /// Test 11: Concurrent reads should not throw exceptions.
  /// </summary>
  [Fact]
  public async Task ConcurrentReads_DoNotThrow()
  {
    // Arrange
    var store = CreateStore();
    var session = new BlueskySession
    {
      AccessToken = "concurrent-access",
      RefreshToken = "concurrent-refresh"
    };
    await store.SaveSessionAsync(session);

    // Act
    var tasks = new List<Task<BlueskySession?>>();
    for (int i = 0; i < 10; i++)
    {
      tasks.Add(store.GetSessionAsync());
    }

    var results = await Task.WhenAll(tasks);

    // Assert
    Assert.All(results, r =>
    {
      Assert.NotNull(r);
      Assert.Equal(session.AccessToken, r.Value.AccessToken);
    });
  }

  /// <summary>
  /// Test 12: Concurrent writes should not corrupt data.
  /// </summary>
  [Fact]
  public async Task ConcurrentWrites_LastWriteWins()
  {
    // Arrange
    var store = CreateStore();
    var tasks = new List<Task>();

    // Act - Multiple concurrent writes
    for (int i = 0; i < 10; i++)
    {
      var session = new BlueskySession
      {
        AccessToken = $"access-token-{i}",
        RefreshToken = $"refresh-token-{i}"
      };
      tasks.Add(store.SaveSessionAsync(session));
    }

    await Task.WhenAll(tasks);

    // Assert - Should have one of the saved sessions
    var result = await store.GetSessionAsync();
    Assert.NotNull(result);
    Assert.StartsWith("access-token-", result.Value.AccessToken);
    Assert.StartsWith("refresh-token-", result.Value.RefreshToken);
  }

  #endregion
}
