using System.Text.Json;
using System.Text.Json.Serialization;
using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Models.Dto;

internal readonly struct BlueskyPostRecordDto
{
    [JsonPropertyName("langs")]
    public string[] Langs { get; init; }

    [JsonPropertyName("text")]
    public string Text { get; init; }

    public static BlueskyPostRecordDto FromJson(string json)
    {
        return JsonSerializer.Deserialize(json, BlueskyDtoContext.Default.BlueskyPostRecordDto);
    }

    public BlueskyPostRecord ToDomain()
    {
        return new BlueskyPostRecord(Langs, Text);
    }
}
