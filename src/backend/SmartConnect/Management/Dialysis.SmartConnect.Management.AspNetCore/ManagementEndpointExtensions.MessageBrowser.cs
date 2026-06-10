using Dialysis.SmartConnect.Documents;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Message browser &amp; statistics routes (<c>/messages</c> get / export / list, <c>/flows/{id}/statistics</c>, reprocess).</summary>
public static partial class ManagementEndpointExtensions
{
    // --- Message Browser & Statistics ---
    internal static void MapMessageBrowserEndpoints(RouteGroupBuilder admin)
    {
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
                    string? messageType,
                    string? senderId,
                    string? batchId,
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
                        // Slice C2: derived ledger columns. Filter values come from the MLLP /
                        // HTTP source connectors (`MSH-9` and `MSH-3@MSH-4` for HL7) — operators
                        // pick from a dropdown / type-ahead the frontend populates from the data.
                        MessageType = string.IsNullOrWhiteSpace(messageType) ? null : messageType,
                        SenderId = string.IsNullOrWhiteSpace(senderId) ? null : senderId,
                        // Slice D2: same shape for the batch id (file path / source identifier
                        // the inbound transport tags on every record of a fan-out).
                        BatchId = string.IsNullOrWhiteSpace(batchId) ? null : batchId,
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
    }
}
