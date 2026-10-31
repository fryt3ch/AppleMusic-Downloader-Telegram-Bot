using System.Text.Json;
using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Converters;

public class TimeSpanMillisecondsConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
            throw new JsonException("Expected number for TimeSpan value.");
        
        var milliseconds = reader.GetInt64();
        
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((long)value.TotalMilliseconds);
    }
}