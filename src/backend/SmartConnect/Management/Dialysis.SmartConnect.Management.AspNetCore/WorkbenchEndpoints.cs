using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>
/// Maps <c>/smartconnect/v1/admin/workbench/*</c> — the HL7 Workbench is an operator tool
/// that lets a user paste a real HL7 v2 message, see the parsed structure, run the same
/// <c>verify-hl7</c> validation pipeline used by route filters, and optionally dispatch the
/// message through any existing HL7v2 flow. No canned data is hosted server-side; everything
/// is operator-provided in the request body.
/// </summary>
public static class WorkbenchEndpoints
{
    private static readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = false };

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>Registers the workbench routes (parse / validate / dispatch).</summary>
        public IEndpointRouteBuilder MapSmartConnectWorkbenchRoutes()
        {
            var group = endpoints.MapGroup("/smartconnect/v1/admin/workbench")
                .WithTags("SmartConnect Workbench");

            group.MapPost(
                    "/parse-hl7",
                    (WorkbenchParseRequest body) =>
                    {
                        if (string.IsNullOrWhiteSpace(body.PayloadText))
                        {
                            return Results.BadRequest(new { error = "payloadText is required." });
                        }
                        try
                        {
                            var parsed = Hl7V2Message.Parse(body.PayloadText);
                            return Results.Ok(new
                            {
                                header = new
                                {
                                    sendingApp = parsed.GetValue("MSH.3"),
                                    sendingFacility = parsed.GetValue("MSH.4"),
                                    receivingApp = parsed.GetValue("MSH.5"),
                                    receivingFacility = parsed.GetValue("MSH.6"),
                                    timestamp = parsed.GetValue("MSH.7"),
                                    messageType = parsed.GetValue("MSH.9"),
                                    trigger = parsed.GetValue("MSH.9.2"),
                                    controlId = parsed.GetValue("MSH.10"),
                                    processingId = parsed.GetValue("MSH.11"),
                                    version = parsed.GetValue("MSH.12"),
                                },
                                segmentsJson = parsed.ToJson(),
                                segmentNames = parsed.Segments
                                    .Select(s => s.Name)
                                    .ToArray(),
                            });
                        }
                        catch (FormatException ex)
                        {
                            return Results.UnprocessableEntity(new { error = ex.Message });
                        }
                        catch (ArgumentException ex)
                        {
                            return Results.UnprocessableEntity(new { error = ex.Message });
                        }
                    })
                .WithName("SmartConnect_Workbench_ParseHl7");

            group.MapPost(
                    "/validate-hl7",
                    (WorkbenchValidateRequest body) =>
                    {
                        if (string.IsNullOrWhiteSpace(body.PayloadText))
                        {
                            return Results.BadRequest(new { error = "payloadText is required." });
                        }

                        var rules = new
                        {
                            requiredSegments = body.RequiredSegments,
                            minVersion = body.MinVersion,
                        };
                        var paramsJson = JsonSerializer.Serialize(rules, _serializerOptions);
                        var metadata = ImmutableDictionary<string, string>.Empty
                            .Add("smartconnect.filter.parameters", paramsJson);

                        var verdict = Transforms.VerifyHl7Core.Inspect(body.PayloadText, metadata);

                        object? parsedHeader = null;
                        string? segmentsJson = null;
                        try
                        {
                            var parsed = Hl7V2Message.Parse(body.PayloadText);
                            parsedHeader = new
                            {
                                trigger = parsed.GetValue("MSH.9.2"),
                                version = parsed.GetValue("MSH.12"),
                                controlId = parsed.GetValue("MSH.10"),
                            };
                            segmentsJson = parsed.ToJson();
                        }
                        catch (FormatException) { /* surfaced via verdict */ }
                        catch (ArgumentException) { /* surfaced via verdict */ }

                        return Results.Ok(new
                        {
                            isValid = verdict.IsValid,
                            reason = verdict.Reason,
                            header = parsedHeader,
                            segmentsJson,
                        });
                    })
                .WithName("SmartConnect_Workbench_ValidateHl7");

            group.MapPost(
                    "/dispatch",
                    async (
                        WorkbenchDispatchRequest body,
                        IIntegrationFlowRepository repo,
                        IFlowRuntime runtime,
                        IMessageLedgerQuery ledgerQuery,
                        CancellationToken ct) =>
                    {
                        if (body.FlowId == Guid.Empty)
                        {
                            return Results.BadRequest(new { error = "flowId is required." });
                        }
                        if (string.IsNullOrWhiteSpace(body.PayloadText))
                        {
                            return Results.BadRequest(new { error = "payloadText is required." });
                        }

                        var flow = await repo.GetByIdAsync(body.FlowId, ct).ConfigureAwait(false);
                        if (flow is null)
                        {
                            return Results.NotFound(new { error = $"Flow {body.FlowId} not found." });
                        }

                        var correlationId = "workbench-" + Guid.NewGuid().ToString("N")[..10];
                        var message = new IntegrationMessage
                        {
                            Id = Guid.NewGuid(),
                            FlowId = flow.Id,
                            CorrelationId = correlationId,
                            Payload = Encoding.UTF8.GetBytes(body.PayloadText),
                            PayloadFormat = PayloadFormat.Utf8Text,
                            ReceivedAtUtc = DateTimeOffset.UtcNow,
                        };

                        var dispatch = await runtime.DispatchAsync(message, ct).ConfigureAwait(false);

                        var (ledger, _) = await ledgerQuery
                            .QueryAsync(
                                new MessageLedgerQueryCriteria
                                {
                                    FlowId = flow.Id,
                                    CorrelationIdPrefix = correlationId,
                                    Take = 100,
                                },
                                ct)
                            .ConfigureAwait(false);

                        return Results.Ok(new
                        {
                            dispatchedMessageId = message.Id,
                            correlationId,
                            succeeded = dispatch.Succeeded,
                            error = dispatch.Error,
                            outboundRoutesAttempted = dispatch.OutboundRoutesAttempted,
                            responsePayload = dispatch.ResponsePayload is null
                                ? null
                                : Encoding.UTF8.GetString(dispatch.ResponsePayload),
                            ledgerSnapshot = ledger.Select(l => new
                            {
                                id = l.Id,
                                status = l.Status.ToString(),
                                outboundRouteOrdinal = l.OutboundRouteOrdinal,
                                detail = l.Detail,
                                createdAtUtc = l.CreatedAtUtc,
                            }),
                        });
                    })
                .WithName("SmartConnect_Workbench_Dispatch");

            return endpoints;
        }
    }

}

/// <summary>Body for <c>POST /admin/workbench/parse-hl7</c>.</summary>
public sealed class WorkbenchParseRequest
{
    public string? PayloadText { get; set; }
}

/// <summary>Body for <c>POST /admin/workbench/validate-hl7</c>.</summary>
public sealed class WorkbenchValidateRequest
{
    public string? PayloadText { get; set; }

    public string[]? RequiredSegments { get; set; }

    public string? MinVersion { get; set; }
}

/// <summary>Body for <c>POST /admin/workbench/dispatch</c>.</summary>
public sealed class WorkbenchDispatchRequest
{
    public Guid FlowId { get; set; }

    public string? PayloadText { get; set; }
}
