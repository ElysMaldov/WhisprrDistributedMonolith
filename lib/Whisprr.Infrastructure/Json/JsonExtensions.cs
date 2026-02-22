using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Whisprr.Infrastructure.Json;

/// <summary>
/// JSON converter for CultureInfo that serializes as BCP-47 language tag string.
/// </summary>
public class CultureInfoJsonConverter : JsonConverter<CultureInfo>
{
    public override CultureInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null ? CultureInfo.InvariantCulture : new CultureInfo(value);
    }

    public override void Write(Utf8JsonWriter writer, CultureInfo value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Name);
    }
}

/// <summary>
/// Extension methods for JSON serialization.
/// </summary>
public static class JsonExtensions
{
    /// <summary>
    /// Configures JSON options with custom converters for domain types.
    /// </summary>
    public static JsonSerializerOptions ConfigureForDomain(this JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new CultureInfoJsonConverter());
        return options;
    }
}
