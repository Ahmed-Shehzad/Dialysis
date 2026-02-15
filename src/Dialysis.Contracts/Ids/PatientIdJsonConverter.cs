using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dialysis.Contracts.Ids;

public sealed class PatientIdJsonConverter : JsonConverter<PatientId>
{
    public override PatientId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        var id = PatientId.TryCreate(s);
        return id ?? default;
    }

    public override void Write(Utf8JsonWriter writer, PatientId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
