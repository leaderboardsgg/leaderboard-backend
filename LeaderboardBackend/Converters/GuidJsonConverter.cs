using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Converters;

public class GuidJsonConverter : JsonConverter<Guid>
{
    public override Guid Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => GuidExtensions.FromUrlSafeBase64String(reader.GetString()!);
    public override void Write(
        Utf8JsonWriter writer,
        Guid value,
        JsonSerializerOptions options
    ) => writer.WriteStringValue(value.ToUrlSafeBase64String());
}
