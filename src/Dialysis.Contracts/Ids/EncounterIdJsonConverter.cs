using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dialysis.Contracts.Ids;

public sealed class EncounterIdJsonConverter : JsonConverter<EncounterId>
{
    public override EncounterId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        var id = EncounterId.TryCreate(s);
        return id ?? default;
    }

    public override void Write(Utf8JsonWriter writer, EncounterId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
