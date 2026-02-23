using Moq;
using StackExchange.Redis;
using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Test.Modules.BlueskySessionStore;

// Alias to avoid namespace conflict with test class namespace
using BlueskyRedisSessionStoreClass = Whisprr.BlueskyService.Modules.BlueskySessionStore.BlueskyRedisSessionStore;

public class BlueskyRedisSessionStoreTest
{
  private readonly Mock<IDatabase> _databaseMock;

  public BlueskyRedisSessionStoreTest()
  {
    _databaseMock = new Mock<IDatabase>();
  }

  private BlueskyRedisSessionStoreClass CreateStore()
  {
    return new BlueskyRedisSessionStoreClass(_databaseMock.Object);
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
    _databaseMock
      .Setup(db => db.HashGetAll(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
      .Returns([]);

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

    _databaseMock
      .Setup(db => db.HashGetAll(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
      .Returns(new HashEntry[]
      {
        new(nameof(session.AccessToken), session.AccessToken),
        new(nameof(session.RefreshToken), session.RefreshToken)
      });

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
    var secondSession = new BlueskySession
    {
      AccessToken = "second-access-token",
      RefreshToken = "second-refresh-token"
    };

    // Simulate that the database returns the second session
    _databaseMock
      .Setup(db => db.HashGetAll(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
      .Returns(new HashEntry[]
      {
        new(nameof(secondSession.AccessToken), secondSession.AccessToken),
        new(nameof(secondSession.RefreshToken), secondSession.RefreshToken)
      });

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

    _databaseMock
      .Setup(db => db.HashGetAll(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
      .Returns(new HashEntry[]
      {
        new(nameof(BlueskySession.AccessToken), accessToken),
        new(nameof(BlueskySession.RefreshToken), refreshToken)
      });

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

    _databaseMock
      .Setup(db => db.HashGetAll(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
      .Returns(new HashEntry[]
      {
        new(nameof(BlueskySession.AccessToken), longAccessToken),
        new(nameof(BlueskySession.RefreshToken), longRefreshToken)
      });

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
    await store.SaveSessionAsync(session);

    // Assert
    _databaseMock.Verify(db => db.HashSet(
      It.IsAny<RedisKey>(),
      It.Is<HashEntry[]>(entries =>
        entries.Any(e => e.Name == nameof(session.AccessToken) && e.Value == session.AccessToken) &&
        entries.Any(e => e.Name == nameof(session.RefreshToken) && e.Value == session.RefreshToken)),
      It.IsAny<CommandFlags>()),
      Times.Once);
  }

  /// <summary>
  /// Test 7: SaveSessionAsync should overwrite existing session.
  /// </summary>
  [Fact]
  public async Task SaveSessionAsync_OverwritesExistingSession()
  {
    // Arrange
    var store = CreateStore();
    var secondSession = new BlueskySession
    {
      AccessToken = "second-access",
      RefreshToken = "second-refresh"
    };

    // Act
    await store.SaveSessionAsync(secondSession);

    // Assert
    _databaseMock.Verify(db => db.HashSet(
      It.IsAny<RedisKey>(),
      It.Is<HashEntry[]>(entries =>
        entries.Any(e => e.Name == nameof(secondSession.AccessToken) && e.Value == secondSession.AccessToken) &&
        entries.Any(e => e.Name == nameof(secondSession.RefreshToken) && e.Value == secondSession.RefreshToken)),
      It.IsAny<CommandFlags>()),
      Times.Once);
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

    // Assert
    _databaseMock.Verify(db => db.HashSet(
      It.IsAny<RedisKey>(),
      It.Is<HashEntry[]>(entries =>
        entries.Any(e => e.Name == nameof(session.AccessToken) && e.Value == "") &&
        entries.Any(e => e.Name == nameof(session.RefreshToken) && e.Value == "")),
      It.IsAny<CommandFlags>()),
      Times.Once);
  }

  #endregion

  #region Integration Tests

  /// <summary>
  /// Test 9: Multiple stores with different database instances should be isolated.
  /// </summary>
  [Fact]
  public async Task MultipleStores_DifferentDatabases_AreIsolated()
  {
    // Arrange
    var dbMock1 = new Mock<IDatabase>();
    var dbMock2 = new Mock<IDatabase>();
    var store1 = new BlueskyRedisSessionStoreClass(dbMock1.Object);
    var store2 = new BlueskyRedisSessionStoreClass(dbMock2.Object);

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

    // Assert - Each store should have saved to its own database
    dbMock1.Verify(db => db.HashSet(
      It.IsAny<RedisKey>(),
      It.Is<HashEntry[]>(entries =>
        entries.Any(e => e.Value == session1.AccessToken)),
      It.IsAny<CommandFlags>()),
      Times.Once);

    dbMock2.Verify(db => db.HashSet(
      It.IsAny<RedisKey>(),
      It.Is<HashEntry[]>(entries =>
        entries.Any(e => e.Value == session2.AccessToken)),
      It.IsAny<CommandFlags>()),
      Times.Once);
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

    _databaseMock
      .Setup(db => db.HashGetAll(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
      .Returns(new HashEntry[]
      {
        new(nameof(session.AccessToken), session.AccessToken),
        new(nameof(session.RefreshToken), session.RefreshToken)
      });

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

    _databaseMock
      .Setup(db => db.HashGetAll(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
      .Returns(new HashEntry[]
      {
        new(nameof(session.AccessToken), session.AccessToken),
        new(nameof(session.RefreshToken), session.RefreshToken)
      });

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

    // Assert - Should have called HashSet 10 times
    _databaseMock.Verify(db => db.HashSet(
      It.IsAny<RedisKey>(),
      It.IsAny<HashEntry[]>(),
      It.IsAny<CommandFlags>()),
      Times.Exactly(10));
  }

  #endregion
}
