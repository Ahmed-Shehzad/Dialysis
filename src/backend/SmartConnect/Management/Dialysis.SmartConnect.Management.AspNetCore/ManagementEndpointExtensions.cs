using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Documents;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Scripts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Maps <c>/smartconnect/v1/admin/*</c> routes for flow lifecycle and import/export.</summary>
public static class ManagementEndpointExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>Registers management endpoints (optionally protected by JWT when configured).</summary>
        public IEndpointRouteBuilder MapSmartConnectManagementRoutes()
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

            admin.MapPost(
                    "/flows/{flowId:guid}/start",
                    async (
                        Guid flowId,
                        bool? force,
                        bool? cascade,
                        IIntegrationFlowRepository repo,
                        ChannelScriptExecutor scripts,
                        CancellationToken ct) =>
                    {
                        var flow = await repo.GetByIdAsync(flowId, ct).ConfigureAwait(false);
                        if (flow is null)
                        {
                            return Results.NotFound();
                        }

                        // Cascade Start — depth-first walk over Dependencies, Starting any flow that
                        // isn't already Started before the requested flow. Cycles refuse with 409 +
                        // the cycle path so the operator can fix the declaration. Cascade implies
                        // force at the leaf level: once we've decided to start a dep, its own deps
                        // also get started, not 422-blocked.
                        if (cascade == true)
                        {
                            var visited = new HashSet<Guid>();
                            var stack = new HashSet<Guid>();
                            var order = new List<IntegrationFlow>();
                            var cyclePath = new List<string>();

                            async Task<bool> WalkAsync(IntegrationFlow current)
                            {
                                if (!stack.Add(current.Id))
                                {
                                    cyclePath.Add($"{current.Name} ({current.Id})");
                                    return false;
                                }
                                foreach (var depId in current.Dependencies)
                                {
                                    if (visited.Contains(depId))
                                    {
                                        continue;
                                    }
                                    var dep = await repo.GetByIdAsync(depId, ct).ConfigureAwait(false);
                                    if (dep is null)
                                    {
                                        continue; // skip missing — Start will fail naturally if it matters
                                    }
                                    if (!await WalkAsync(dep).ConfigureAwait(false))
                                    {
                                        cyclePath.Add($"{current.Name} ({current.Id})");
                                        return false;
                                    }
                                }
                                stack.Remove(current.Id);
                                visited.Add(current.Id);
                                order.Add(current);
                                return true;
                            }

                            if (!await WalkAsync(flow).ConfigureAwait(false))
                            {
                                cyclePath.Reverse();
                                return Results.Conflict(new
                                {
                                    error = "Dependency cycle detected; cannot cascade-start.",
                                    cyclePath,
                                });
                            }

                            var started = new List<object>();
                            foreach (var f in order)
                            {
                                if (f.RuntimeState == FlowRuntimeState.Started)
                                {
                                    continue;
                                }
                                if (!string.IsNullOrWhiteSpace(f.Pipeline.Scripts?.DeployScript))
                                {
                                    scripts.RunLifecycleScript(f.Pipeline.Scripts.DeployScript!, f.Id);
                                }
                                var setOk = await repo.SetRuntimeStateAsync(f.Id, FlowRuntimeState.Started, ct)
                                    .ConfigureAwait(false);
                                if (setOk)
                                {
                                    started.Add(new { id = f.Id, name = f.Name });
                                }
                            }

                            return Results.Ok(new { started, count = started.Count });
                        }

                        // Dependency enforcement — refuse to Start unless every declared dependency is
                        // already Started, OR ?force=true is supplied (operator override for a known
                        // out-of-order startup, e.g. recovering after a crash).
                        if (force != true && flow.Dependencies.Count > 0)
                        {
                            var unmet = new List<object>();
                            foreach (var depId in flow.Dependencies)
                            {
                                var dep = await repo.GetByIdAsync(depId, ct).ConfigureAwait(false);
                                if (dep is null)
                                {
                                    unmet.Add(new { id = depId, name = (string?)null, state = "missing" });
                                }
                                else if (dep.RuntimeState != FlowRuntimeState.Started)
                                {
                                    unmet.Add(new
                                    {
                                        id = dep.Id,
                                        name = dep.Name,
                                        state = dep.RuntimeState.ToString(),
                                    });
                                }
                            }

                            if (unmet.Count > 0)
                            {
                                return Results.UnprocessableEntity(new
                                {
                                    error = $"Cannot start flow '{flow.Name}': {unmet.Count} declared dependency/dependencies are not Started. Start them first or pass ?force=true.",
                                    unmetDependencies = unmet,
                                });
                            }
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

            // Channel-attachment blob endpoints — let operators upload reference docs that exceed the
            // 1 MiB inline cap (PDFs, large profile bundles, sample DICOM, …). Bytes are persisted
            // through IAttachmentStore (same atomic metadata+bytes write the per-message attachment
            // store uses); MessageId is set to the flow id since channel attachments aren't tied to
            // a message. The returned ref is then dropped into the channel's Attachments array with
            // `storageRef: { kind: "blob", id: ... }`.
            admin.MapPost(
                    "/flows/{flowId:guid}/attachments/blob",
                    async (
                        Guid flowId,
                        HttpRequest request,
                        IIntegrationFlowRepository repo,
                        IAttachmentStore attachments,
                        CancellationToken ct) =>
                    {
                        if (await repo.GetByIdAsync(flowId, ct).ConfigureAwait(false) is null)
                        {
                            return Results.NotFound(new { error = $"Flow {flowId} not found." });
                        }
                        await using var buffer = new MemoryStream();
                        await request.Body.CopyToAsync(buffer, ct).ConfigureAwait(false);
                        if (buffer.Length == 0)
                        {
                            return Results.BadRequest(new { error = "Request body is empty." });
                        }
                        var data = buffer.ToArray();
                        var blobId = Guid.NewGuid();
                        var mime = request.ContentType ?? "application/octet-stream";
                        await attachments.AddAsync(new Attachment
                        {
                            Id = blobId,
                            FlowId = flowId,
                            MessageId = flowId, // synthetic: channel attachments don't belong to a message
                            MimeType = mime,
                            Data = data,
                            SizeBytes = data.Length,
                            CreatedUtc = DateTimeOffset.UtcNow,
                        }, ct).ConfigureAwait(false);
                        return Results.Ok(new
                        {
                            storageRef = new { kind = "blob", id = blobId, sizeBytes = data.Length },
                        });
                    })
                .WithName("SmartConnect_UploadChannelAttachmentBlob");

            admin.MapGet(
                    "/flows/{flowId:guid}/attachments/blob/{blobId:guid}",
                    async (
                        Guid flowId,
                        Guid blobId,
                        string? mimeType,
                        IIntegrationFlowRepository repo,
                        IAttachmentStore attachments,
                        CancellationToken ct) =>
                    {
                        var flow = await repo.GetByIdAsync(flowId, ct).ConfigureAwait(false);
                        if (flow is null)
                        {
                            return Results.NotFound(new { error = $"Flow {flowId} not found." });
                        }
                        // Authorise via the channel: the blob must be referenced by one of the flow's
                        // declared attachments. Anything else would let operators read every blob in
                        // the store by guessing ids.
                        var attRef = flow.Attachments.FirstOrDefault(a =>
                            a.StorageRef is not null && a.StorageRef.Id == blobId);
                        if (attRef is null)
                        {
                            return Results.NotFound(new { error = "Blob is not referenced by this channel." });
                        }
                        var att = await attachments.GetAsync(blobId, ct).ConfigureAwait(false);
                        if (att is null)
                        {
                            return Results.NotFound(new { error = $"Blob {blobId} not found in the store." });
                        }
                        var contentType = string.IsNullOrWhiteSpace(mimeType) ? attRef.MimeType : mimeType!;
                        return Results.File(att.Data.ToArray(), contentType, attRef.Name);
                    })
                .WithName("SmartConnect_DownloadChannelAttachmentBlob");

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

            // --- Message Browser & Statistics ---

            admin.MapGet(
                    "/messages/{ledgerEntryId:guid}",
                    async (Guid ledgerEntryId, IMessageLedgerQuery ledgerQuery, CancellationToken ct) =>
                    {
                        var entry = await ledgerQuery.GetByIdAsync(ledgerEntryId, ct).ConfigureAwait(false);
                        return entry is null ? Results.NotFound() : Results.Ok(entry);
                    })
                .WithName("SmartConnect_GetMessage");

            // Convert a captured payload into a downloadable clinical document. Supported
            // ?format= values: raw, hl7, xml (HL7 v2 XML), cda (C-CDA R2.1 CCD), fhir (R4 Bundle).
            admin.MapGet(
                    "/messages/{ledgerEntryId:guid}/export",
                    async (
                        Guid ledgerEntryId,
                        string? format,
                        IMessageLedgerQuery ledgerQuery,
                        CancellationToken ct) =>
                    {
                        var entry = await ledgerQuery.GetByIdAsync(ledgerEntryId, ct).ConfigureAwait(false);
                        if (entry is null)
                        {
                            return Results.NotFound();
                        }

                        if (entry.PayloadSnapshot is null || entry.PayloadSnapshot.Length == 0)
                        {
                            return Results.BadRequest(new
                            {
                                error = "Ledger entry has no captured payload to export " +
                                    "(snapshots are recorded only at the Received and OutboundFailed stages).",
                            });
                        }

                        try
                        {
                            var doc = MessageDocumentExporter.Export(
                                entry.PayloadSnapshot, format, entry.CorrelationId);
                            return Results.File(doc.Content, doc.ContentType, doc.FileName);
                        }
                        catch (ArgumentException ex)
                        {
                            return Results.BadRequest(new { error = ex.Message });
                        }
                    })
                .WithName("SmartConnect_ExportMessage");

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

            // Slice B2: connector schema endpoints. List every registered outbound adapter
            // kind + (when published) its parameter JSON Schema so the operator-shell can
            // render a form-driven editor instead of raw-JSON. Adapters that haven't
            // published a schema appear in the list with hasSchema:false.
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

            // Debug: evaluate a JavaScript snippet against a stubbed HL7 payload through the same
            // binding path the real JavascriptTransformStage uses. Engineers can sanity-check a
            // transformer before deploying it, instead of round-tripping through send-message +
            // inspect-ledger. Body: { "script": "msg.GetValue('PID.3.1')", "payloadText": "MSH|..." }.
            // Response: { "result": "MRN-12345" } on success, { "error": "..." } on failure.
            admin.MapPost(
                    "/debug/evaluate-script",
                    async (
                        EvaluateScriptRequest body,
                        IServiceScopeFactory scopeFactory,
                        CancellationToken ct) =>
                    {
                        if (string.IsNullOrWhiteSpace(body.Script))
                        {
                            return Results.BadRequest(new { error = "script is required." });
                        }

                        var parameters = JsonSerializer.Serialize(new { script = body.Script });
                        var payloadBytes = Encoding.UTF8.GetBytes(body.PayloadText ?? string.Empty);
                        var stub = new IntegrationMessage
                        {
                            Id = Guid.NewGuid(),
                            FlowId = Guid.Empty,
                            CorrelationId = "debug-" + Guid.NewGuid().ToString("N")[..8],
                            Payload = payloadBytes,
                            PayloadFormat = PayloadFormat.Utf8Text,
                            Metadata = ImmutableDictionary<string, string>.Empty.Add(
                                JavascriptTransformStage.ParametersMetadataKey,
                                parameters),
                            ReceivedAtUtc = DateTimeOffset.UtcNow,
                        };

                        try
                        {
                            // Resolve the stage from a fresh DI scope so the captured IServiceProvider
                            // inside JavascriptTransformStage can resolve Scoped variable-map services
                            // (IVariableMapStore, ICodeTemplateLibraryRepository, etc.).
                            await using var scope = scopeFactory.CreateAsyncScope();
                            var stage = new JavascriptTransformStage(scope.ServiceProvider);
                            var result = await stage.TransformAsync(stub, ct).ConfigureAwait(false);
                            return Results.Ok(new
                            {
                                result = Encoding.UTF8.GetString(result.Payload.Span),
                            });
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            return Results.UnprocessableEntity(new
                            {
                                error = ex.Message,
                                type = ex.GetType().Name,
                            });
                        }
                    })
                .WithName("SmartConnect_EvaluateScript");

            return endpoints;
        }
    }
}

/// <summary>Request body for <c>POST /smartconnect/v1/admin/debug/evaluate-script</c>.</summary>
public sealed class EvaluateScriptRequest
{
    public string? Script { get; set; }

    public string? PayloadText { get; set; }
}
