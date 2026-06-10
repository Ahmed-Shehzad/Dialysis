using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Connector schema routes (<c>/connectors/outbound</c> list, <c>/connectors/outbound/{kind}/schema</c>).</summary>
public static partial class ManagementEndpointExtensions
{
    // Slice B2: connector schema endpoints. List every registered outbound adapter
    // kind + (when published) its parameter JSON Schema so the operator-shell can
    // render a form-driven editor instead of raw-JSON. Adapters that haven't
    // published a schema appear in the list with hasSchema:false.
    internal static void MapConnectorSchemaEndpoints(RouteGroupBuilder admin)
    {
        admin.MapGet(
                "/connectors/outbound",
                (IFlowPluginRegistry registry) =>
                {
                    var items = registry
                        .EnumerateOutboundAdapters()
                        .Select(a => new { kind = a.Kind, hasSchema = a.GetParametersSchema() is not null })
                        .OrderBy(x => x.kind, StringComparer.Ordinal)
                        .ToArray();
                    return Results.Ok(items);
                })
            .WithName("SmartConnect_ListOutboundConnectors");

        admin.MapGet(
                "/connectors/outbound/{kind}/schema",
                (string kind, IFlowPluginRegistry registry) =>
                {
                    var adapter = registry.TryResolveOutboundAdapter(kind);
                    if (adapter is null)
                    {
                        return Results.NotFound(new { error = $"No outbound adapter registered for kind '{kind}'." });
                    }
                    var schema = adapter.GetParametersSchema();
                    return schema is null
                        ? Results.NotFound(new { error = $"Adapter '{kind}' has not published a parameters schema." })
                        : Results.Content(schema, "application/schema+json");
                })
            .WithName("SmartConnect_GetOutboundConnectorSchema");
    }
}
