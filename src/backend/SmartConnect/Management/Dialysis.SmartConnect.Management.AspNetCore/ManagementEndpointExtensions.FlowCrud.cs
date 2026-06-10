using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Flow CRUD routes (<c>/flows</c> list / get / create / update / delete).</summary>
public static partial class ManagementEndpointExtensions
{
    internal static void MapFlowCrudEndpoints(RouteGroupBuilder admin)
    {
        admin.MapGet(
                "/flows",
                async (IIntegrationFlowRepository repo, CancellationToken ct) =>
                {
                    var flows = await repo.GetAllAsync(ct).ConfigureAwait(false);
                    return Results.Ok(flows);
                })
            .WithName("SmartConnect_ListFlows");

        admin.MapGet(
                "/flows/{flowId:guid}",
                async (Guid flowId, IIntegrationFlowRepository repo, CancellationToken ct) =>
                {
                    var flow = await repo.GetByIdAsync(flowId, ct).ConfigureAwait(false);
                    return flow is null ? Results.NotFound() : Results.Ok(flow);
                })
            .WithName("SmartConnect_GetFlow");

        admin.MapPost(
                "/flows",
                async (
                    IntegrationFlow body,
                    IIntegrationFlowRepository repo,
                    IFlowPluginRegistry registry,
                    CancellationToken ct) =>
                {
                    try
                    {
                        PipelineValidation.ValidateOrThrow(body.Pipeline, registry);
                        PipelineValidation.ValidateChannelMetadataOrThrow(body);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.BadRequest(new { error = ex.Message });
                    }

                    if (body.Id == Guid.Empty)
                    {
                        return Results.BadRequest(new { error = "Flow Id is required." });
                    }

                    foreach (var depId in body.Dependencies)
                    {
                        if (await repo.GetByIdAsync(depId, ct).ConfigureAwait(false) is null)
                        {
                            return Results.BadRequest(new
                            {
                                error = $"Dependency flow id {depId} does not exist.",
                            });
                        }
                    }

                    if (await repo.GetByIdAsync(body.Id, ct).ConfigureAwait(false) is not null)
                    {
                        return Results.Conflict(new { error = "Flow already exists." });
                    }

                    await repo.AddAsync(body, ct).ConfigureAwait(false);
                    return Results.Created($"/api/v1/admin/flows/{body.Id}", body);
                })
            .WithName("SmartConnect_CreateFlow");

        admin.MapPut(
                "/flows/{flowId:guid}",
                async (
                    Guid flowId,
                    IntegrationFlow body,
                    IIntegrationFlowRepository repo,
                    IFlowPluginRegistry registry,
                    CancellationToken ct) =>
                {
                    if (body.Id != flowId)
                    {
                        return Results.BadRequest(new { error = "Route id and body id must match." });
                    }

                    try
                    {
                        PipelineValidation.ValidateOrThrow(body.Pipeline, registry);
                        PipelineValidation.ValidateChannelMetadataOrThrow(body);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.BadRequest(new { error = ex.Message });
                    }

                    foreach (var depId in body.Dependencies)
                    {
                        if (await repo.GetByIdAsync(depId, ct).ConfigureAwait(false) is null)
                        {
                            return Results.BadRequest(new
                            {
                                error = $"Dependency flow id {depId} does not exist.",
                            });
                        }
                    }

                    var ok = await repo.UpdateAsync(body, ct).ConfigureAwait(false);
                    return ok ? Results.NoContent() : Results.NotFound();
                })
            .WithName("SmartConnect_UpdateFlow");

        admin.MapDelete(
                "/flows/{flowId:guid}",
                async (Guid flowId, IIntegrationFlowRepository repo, CancellationToken ct) =>
                {
                    var ok = await repo.DeleteAsync(flowId, ct).ConfigureAwait(false);
                    return ok ? Results.NoContent() : Results.NotFound();
                })
            .WithName("SmartConnect_DeleteFlow");
    }
}
