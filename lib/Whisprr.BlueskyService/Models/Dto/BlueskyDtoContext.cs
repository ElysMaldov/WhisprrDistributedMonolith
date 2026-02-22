using System.Text.Json.Serialization;
using Whisprr.BlueskyService.Models.Dto;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false)]
[JsonSerializable(typeof(SearchPostDto))]
[JsonSerializable(typeof(BlueskyAuthorDto))]
[JsonSerializable(typeof(BlueskyPostRecordDto))]
[JsonSerializable(typeof(SessionDto))]
internal partial class BlueskyDtoContext : JsonSerializerContext
{
}