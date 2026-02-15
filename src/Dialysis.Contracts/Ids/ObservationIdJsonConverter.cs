using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dialysis.Contracts.Ids;

public sealed class ObservationIdJsonConverter : JsonConverter<ObservationId>
{
    public override ObservationId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        var id = ObservationId.TryCreate(s);
        return id ?? default;
    }

    public override void Write(Utf8JsonWriter writer, ObservationId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
