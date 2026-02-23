namespace Whisprr.BlueskyService.Models.Domain;

public readonly struct BlueskyAuthor(
    string dId,
    string handle,
    string displayName,
    string avatar,
    string[] labels,
    DateTimeOffset createdAt)
{
    public string DId { get; } = dId;
    public string Handle { get; } = handle;
    public string DisplayName { get; } = displayName;
    public string Avatar { get; } = avatar;
    public string[] Labels { get; } = labels;
    public DateTimeOffset CreatedAt { get; } = createdAt;
}
