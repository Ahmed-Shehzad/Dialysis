using System.Text.Json;
using Dialysis.SmartConnect;
using Dialysis.SmartConnect.Persistence;
using Dialysis.SmartConnect.Scripts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Maps <c>/smartconnect/v1/admin/*</c> routes for flow lifecycle and import/export.</summary>
public static class ManagementEndpointExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Registers management endpoints (optionally protected by JWT when configured).</summary>
    public static IEndpointRouteBuilder MapSmartConnectManagementRoutes(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/smartconnect/v1/admin").WithTags("SmartConnect Admin");

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
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.BadRequest(new { error = ex.Message });
                    }

                    if (body.Id == Guid.Empty)
                    {
                        return Results.BadRequest(new { error = "Flow Id is required." });
                    }

                    if (await repo.GetByIdAsync(body.Id, ct).ConfigureAwait(false) is not null)
                    {
                        return Results.Conflict(new { error = "Flow already exists." });
                    }

                    await repo.AddAsync(body, ct).ConfigureAwait(false);
                    return Results.Created($"/smartconnect/v1/admin/flows/{body.Id}", body);
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
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.BadRequest(new { error = ex.Message });
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

        admin.MapPost(
                "/flows/{flowId:guid}/start",
                async (Guid flowId, IIntegrationFlowRepository repo, ChannelScriptExecutor scripts, CancellationToken ct) =>
                {
                    var flow = await repo.GetByIdAsync(flowId, ct).ConfigureAwait(false);
                    if (flow is null)
                    {
                        return Results.NotFound();
                    }

                    if (!string.IsNullOrWhiteSpace(flow.Pipeline.Scripts?.DeployScript))
                    {
                        scripts.RunLifecycleScript(flow.Pipeline.Scripts.DeployScript!, flowId);
                    }

                    var ok = await repo.SetRuntimeStateAsync(flowId, FlowRuntimeState.Started, ct)
                        .ConfigureAwait(false);
                    return ok ? Results.NoContent() : Results.NotFound();
                })
            .WithName("SmartConnect_StartFlow");

        admin.MapPost(
                "/flows/{flowId:guid}/stop",
                async (Guid flowId, IIntegrationFlowRepository repo, ChannelScriptExecutor scripts, CancellationToken ct) =>
                {
                    var flow = await repo.GetByIdAsync(flowId, ct).ConfigureAwait(false);
                    if (flow is null)
                    {
                        return Results.NotFound();
                    }

                    if (!string.IsNullOrWhiteSpace(flow.Pipeline.Scripts?.UndeployScript))
                    {
                        scripts.RunLifecycleScript(flow.Pipeline.Scripts.UndeployScript!, flowId);
                    }

                    var ok = await repo.SetRuntimeStateAsync(flowId, FlowRuntimeState.Stopped, ct)
                        .ConfigureAwait(false);
                    return ok ? Results.NoContent() : Results.NotFound();
                })
            .WithName("SmartConnect_StopFlow");

        admin.MapPost(
                "/flows/{flowId:guid}/pause",
                async (Guid flowId, IIntegrationFlowRepository repo, CancellationToken ct) =>
                {
                    var ok = await repo.SetRuntimeStateAsync(flowId, FlowRuntimeState.Paused, ct)
                        .ConfigureAwait(false);
                    return ok ? Results.NoContent() : Results.NotFound();
                })
            .WithName("SmartConnect_PauseFlow");

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
                        flow = JsonSerializer.Deserialize<IntegrationFlow>(ms.ToArray(), JsonOptions);
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

                    var json = JsonSerializer.Serialize(flow, JsonOptions);
                    return Results.Text(json, "application/json");
                })
            .WithName("SmartConnect_ExportFlow");

        // --- Message Browser & Statistics ---

        admin.MapGet(
                "/messages/{ledgerEntryId:guid}",
                async (Guid ledgerEntryId, IMessageLedgerQuery ledgerQuery, CancellationToken ct) =>
                {
                    var entry = await ledgerQuery.GetByIdAsync(ledgerEntryId, ct).ConfigureAwait(false);
                    return entry is null ? Results.NotFound() : Results.Ok(entry);
                })
            .WithName("SmartConnect_GetMessage");

        admin.MapGet(
                "/messages",
                async (
                    IMessageLedgerQuery ledgerQuery,
                    Guid? flowId,
                    string? correlationIdPrefix,
                    DateTimeOffset? from,
                    DateTimeOffset? to,
                    int? status,
                    int? skip,
                    int? take,
                    CancellationToken ct) =>
                {
                    MessageLedgerStatus? st = status is >= 0 ? (MessageLedgerStatus)status.Value : null;
                    var criteria = new MessageLedgerQueryCriteria
                    {
                        FlowId = flowId,
                        CorrelationIdPrefix = correlationIdPrefix,
                        CreatedFromUtc = from,
                        CreatedToUtc = to,
                        Status = st,
                        Skip = skip ?? 0,
                        Take = take ?? 50,
                    };
                    var (items, total) = await ledgerQuery.QueryAsync(criteria, ct).ConfigureAwait(false);
                    return Results.Ok(new { items, totalCount = total });
                })
            .WithName("SmartConnect_ListMessages");

        admin.MapGet(
                "/flows/{flowId:guid}/statistics",
                async (Guid flowId, IMessageLedgerStatistics stats, CancellationToken ct) =>
                {
                    var result = await stats.GetFlowStatisticsAsync(flowId, ct).ConfigureAwait(false);
                    return Results.Ok(result);
                })
            .WithName("SmartConnect_FlowStatistics");

        admin.MapPost(
                "/messages/{ledgerEntryId:guid}/reprocess",
                async (
                    Guid ledgerEntryId,
                    IMessageLedgerQuery ledgerQuery,
                    IFlowRuntime runtime,
                    CancellationToken ct) =>
                {
                    var entry = await ledgerQuery.GetByIdAsync(ledgerEntryId, ct).ConfigureAwait(false);

                    if (entry is null)
                    {
                        return Results.NotFound(new { error = "Ledger entry not found." });
                    }

                    if (entry.PayloadSnapshot is null || entry.PayloadSnapshot.Length == 0)
                    {
                        return Results.BadRequest(new { error = "Ledger entry has no payload snapshot to reprocess." });
                    }

                    var message = new IntegrationMessage
                    {
                        Id = Guid.NewGuid(),
                        FlowId = entry.FlowId,
                        CorrelationId = entry.CorrelationId,
                        Payload = entry.PayloadSnapshot,
                        PayloadFormat = PayloadFormat.Utf8Text,
                        ReceivedAtUtc = DateTimeOffset.UtcNow,
                    };

                    var result = await runtime.DispatchAsync(message, ct).ConfigureAwait(false);
                    return result.Succeeded
                        ? Results.Ok(new { reprocessedMessageId = message.Id })
                        : Results.UnprocessableEntity(new { error = result.Error });
                })
            .WithName("SmartConnect_ReprocessMessage");

        return endpoints;
    }
}
