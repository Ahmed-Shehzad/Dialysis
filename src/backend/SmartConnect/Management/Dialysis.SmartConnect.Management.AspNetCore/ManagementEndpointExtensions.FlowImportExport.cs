using System.Text.Json;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Flow import/export routes (<c>/flows/import</c>, <c>/flows/{id}/export</c>).</summary>
public static partial class ManagementEndpointExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    internal static void MapFlowImportExportEndpoints(RouteGroupBuilder admin)
    {
        admin.MapPost(
                "/flows/import",
                async (
                    HttpRequest request,
                    IIntegrationFlowRepository repo,
                    IFlowPluginRegistry registry,
                    CancellationToken ct) =>
                {
                    await using var ms = new MemoryStream();
                    await request.Body.CopyToAsync(ms, ct).ConfigureAwait(false);
                    IntegrationFlow? flow;
                    try
                    {
                        flow = JsonSerializer.Deserialize<IntegrationFlow>(ms.ToArray(), _jsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        return Results.BadRequest(new { error = ex.Message });
                    }

                    if (flow is null || flow.Id == Guid.Empty)
                    {
                        return Results.BadRequest(new { error = "Invalid integration flow JSON." });
                    }

                    try
                    {
                        PipelineValidation.ValidateOrThrow(flow.Pipeline, registry);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.BadRequest(new { error = ex.Message });
                    }

                    if (await repo.GetByIdAsync(flow.Id, ct).ConfigureAwait(false) is not null)
                    {
                        await repo.UpdateAsync(flow, ct).ConfigureAwait(false);
                    }
                    else
                    {
                        await repo.AddAsync(flow, ct).ConfigureAwait(false);
                    }

                    return Results.Ok(flow);
                })
            .WithName("SmartConnect_ImportFlow");

        admin.MapGet(
                "/flows/{flowId:guid}/export",
                async (Guid flowId, IIntegrationFlowRepository repo, CancellationToken ct) =>
                {
                    var flow = await repo.GetByIdAsync(flowId, ct).ConfigureAwait(false);
                    if (flow is null)
                    {
                        return Results.NotFound();
                    }

                    var json = JsonSerializer.Serialize(flow, _jsonOptions);
                    return Results.Text(json, "application/json");
                })
            .WithName("SmartConnect_ExportFlow");
    }
}
