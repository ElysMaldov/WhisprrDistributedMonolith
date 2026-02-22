namespace Whisprr.BlueskyService.Models.Domain;

public readonly struct BlueskyPostRecord(
    string[] langs,
    string text)
{
    public string[] Langs { get; } = langs;
    public string Text { get; } = text;
}
