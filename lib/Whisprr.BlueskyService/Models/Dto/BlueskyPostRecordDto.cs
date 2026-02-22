using System.Text.Json;
using Whisprr.BlueskyService.Models.Domain;

namespace Whisprr.BlueskyService.Models.Dto;

public readonly struct BlueskyPostRecordDto(string[] langs, string text)
{
  public string[] Langs { get; } = langs;
  public string Text { get; } = text;

  public static BlueskyPostRecordDto FromJson(string json)
  {
    return JsonSerializer.Deserialize(json, BlueskyDtoContext.Default.BlueskyPostRecordDto);
  }

  public BlueskyPostRecord ToDomain()
  {
    return new BlueskyPostRecord(Langs, Text);
  }
}