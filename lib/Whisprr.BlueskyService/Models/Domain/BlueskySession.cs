namespace Whisprr.BlueskyService.Models.Domain;

/// <summary>
/// We seperate domain from DTOs to make our program decoupled from the API.
/// If the API structure changes, we would need to update our struct to
/// follow the new json, which might break other parts of our app. Using
/// a domain model like this reduce that breakage by narrowing it down
/// to only when mapping between the JSON and Domain model.
/// </summary>
public readonly struct BlueskySession
{
  public required string AccessToken { get; init; }
  public required string RefreshToken { get; init; }
}