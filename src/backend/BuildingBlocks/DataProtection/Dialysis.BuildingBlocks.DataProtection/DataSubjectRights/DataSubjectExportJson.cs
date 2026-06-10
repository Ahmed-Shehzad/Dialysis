using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;

/// <summary>
/// Shared JSON shape for the per-module GDPR Art. 15 / 20 exports. Every
/// <see cref="IModuleDataExtractor"/> serializes its patient-linked rows through this helper so
/// the stitched <see cref="DataSubjectExport"/> bundle is uniform across modules:
/// camelCase property names, enums as strings (machine-readable per Art. 20 without leaking
/// internal ordinal values), and cycles ignored so EF navigation shapes can't blow the export.
/// Aggregate infrastructure (<c>DomainEvents</c> / <c>IntegrationEvents</c> collections on the
/// DDD aggregate-root base) is stripped — it is plumbing, not personal data.
/// </summary>
public static class DataSubjectExportJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    /// <summary>Serializes one module row into the export's canonical JSON form.</summary>
    public static string Serialize<T>(T row) => JsonSerializer.Serialize(row, Options);

    private static JsonSerializerOptions CreateOptions() => new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { StripAggregateInfrastructure },
        },
    };

    private static void StripAggregateInfrastructure(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;
        for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
        {
            var name = typeInfo.Properties[i].Name;
            if (name.Equals("domainEvents", StringComparison.OrdinalIgnoreCase)
                || name.Equals("integrationEvents", StringComparison.OrdinalIgnoreCase))
            {
                typeInfo.Properties.RemoveAt(i);
            }
        }
    }
}
