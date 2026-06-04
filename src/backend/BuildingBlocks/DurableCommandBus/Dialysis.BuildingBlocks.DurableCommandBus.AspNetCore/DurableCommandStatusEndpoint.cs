using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.DurableCommandBus.AspNetCore;

/// <summary>
/// HTTP surface for ledger reads. Mounts <c>GET {prefix}/{correlationId}</c> on the host's
/// route table. Returns <see cref="DurableCommandStatusResponse"/>; 404 when the correlation
/// id is unknown; 403 when the caller's <c>sub</c> doesn't match the row's
/// <c>RequestedBySubject</c>, so a correlation id leaked between tenants isn't a probe vector.
/// </summary>
public static class DurableCommandStatusEndpointExtensions
{
    public static IEndpointRouteBuilder MapDurableCommandStatusEndpoint(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);
        var options = routes.ServiceProvider.GetRequiredService<IOptions<DurableCommandBusOptions>>().Value;
        var prefix = options.StatusEndpointPrefix.TrimEnd('/');
        routes.MapGet(
            $"{prefix}/{{correlationId}}",
            async (string correlationId, HttpContext http) =>
            {
                var ledger = http.RequestServices.GetRequiredService<ICommandLedger>();
                var entry = await ledger.FindByCorrelationAsync(correlationId, http.RequestAborted)
                    .ConfigureAwait(false);
                if (entry is null)
                    return Results.NotFound();

                if (!string.IsNullOrEmpty(entry.RequestedBySubject))
                {
                    var subject = http.User.FindFirstValue("sub")
                        ?? http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.Equals(subject, entry.RequestedBySubject, StringComparison.Ordinal))
                        return Results.Forbid();
                }

                JsonElement? resultElement = null;
                if (!string.IsNullOrWhiteSpace(entry.ResultJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(entry.ResultJson);
                        resultElement = doc.RootElement.Clone();
                    }
                    catch (JsonException)
                    {
                        resultElement = null;
                    }
                }

                JsonElement? failureElement = null;
                if (!string.IsNullOrWhiteSpace(entry.FailureJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(entry.FailureJson);
                        failureElement = doc.RootElement.Clone();
                    }
                    catch (JsonException)
                    {
                        failureElement = null;
                    }
                }

                return Results.Json(new DurableCommandStatusResponse(
                    CommandId: entry.CommandId,
                    CorrelationId: entry.CorrelationId,
                    Status: entry.Status.ToString(),
                    EnqueuedAtUtc: entry.EnqueuedAtUtc,
                    AppliedAtUtc: entry.AppliedAtUtc,
                    Result: resultElement,
                    Failure: failureElement));
            });
        return routes;
    }
}

/// <summary>HTTP wire shape for the status endpoint.</summary>
public sealed record DurableCommandStatusResponse
{
    /// <summary>HTTP wire shape for the status endpoint.</summary>
    public DurableCommandStatusResponse(Guid CommandId,
        string CorrelationId,
        string Status,
        DateTime EnqueuedAtUtc,
        DateTime? AppliedAtUtc,
        JsonElement? Result,
        JsonElement? Failure)
    {
        this.CommandId = CommandId;
        this.CorrelationId = CorrelationId;
        this.Status = Status;
        this.EnqueuedAtUtc = EnqueuedAtUtc;
        this.AppliedAtUtc = AppliedAtUtc;
        this.Result = Result;
        this.Failure = Failure;
    }
    public Guid CommandId { get; init; }
    public string CorrelationId { get; init; }
    public string Status { get; init; }
    public DateTime EnqueuedAtUtc { get; init; }
    public DateTime? AppliedAtUtc { get; init; }
    public JsonElement? Result { get; init; }
    public JsonElement? Failure { get; init; }
    public void Deconstruct(out Guid CommandId, out string CorrelationId, out string Status, out DateTime EnqueuedAtUtc, out DateTime? AppliedAtUtc, out JsonElement? Result, out JsonElement? Failure)
    {
        CommandId = this.CommandId;
        CorrelationId = this.CorrelationId;
        Status = this.Status;
        EnqueuedAtUtc = this.EnqueuedAtUtc;
        AppliedAtUtc = this.AppliedAtUtc;
        Result = this.Result;
        Failure = this.Failure;
    }
}
