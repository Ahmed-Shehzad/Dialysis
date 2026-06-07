using Dialysis.SmartConnect.Alerts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>
/// Maps <c>/api/v1/admin/alert-rules/*</c> and <c>/api/v1/admin/alert-events/*</c>
/// for rule CRUD, history retrieval, and a synthetic test endpoint.
/// </summary>
public static class AlertEndpointExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapSmartConnectAlertRoutes()
        {
            var rules = endpoints.MapGroup("/api/v1/admin/alert-rules").WithTags("SmartConnect Admin");

            rules.MapGet(
                    "/",
                    async (IAlertRuleRepository repo, bool? enabledOnly, CancellationToken ct) =>
                    {
                        var list = enabledOnly is true
                            ? await repo.GetEnabledAsync(ct).ConfigureAwait(false)
                            : await repo.GetAllAsync(ct).ConfigureAwait(false);
                        return Results.Ok(list);
                    })
                .WithName("SmartConnect_ListAlertRules");

            rules.MapGet(
                    "/{id:guid}",
                    async (Guid id, IAlertRuleRepository repo, CancellationToken ct) =>
                    {
                        var rule = await repo.GetByIdAsync(id, ct).ConfigureAwait(false);
                        return rule is null ? Results.NotFound() : Results.Ok(rule);
                    })
                .WithName("SmartConnect_GetAlertRule");

            rules.MapPost(
                    "/",
                    async (AlertRule body, IAlertRuleRepository repo, CancellationToken ct) =>
                    {
                        var id = body.Id == Guid.Empty ? Guid.CreateVersion7() : body.Id;
                        var rule = WithId(body, id);
                        await repo.UpsertAsync(rule, ct).ConfigureAwait(false);
                        return Results.Created($"/api/v1/admin/alert-rules/{id}", rule);
                    })
                .WithName("SmartConnect_CreateAlertRule");

            rules.MapPut(
                    "/{id:guid}",
                    async (Guid id, AlertRule body, IAlertRuleRepository repo, CancellationToken ct) =>
                    {
                        var existing = await repo.GetByIdAsync(id, ct).ConfigureAwait(false);
                        if (existing is null)
                            return Results.NotFound();
                        var rule = WithId(body, id);
                        await repo.UpsertAsync(rule, ct).ConfigureAwait(false);
                        return Results.Ok(rule);
                    })
                .WithName("SmartConnect_UpdateAlertRule");

            rules.MapDelete(
                    "/{id:guid}",
                    async (Guid id, IAlertRuleRepository repo, CancellationToken ct) =>
                    {
                        await repo.DeleteAsync(id, ct).ConfigureAwait(false);
                        return Results.NoContent();
                    })
                .WithName("SmartConnect_DeleteAlertRule");

            rules.MapPost(
                    "/{id:guid}/test",
                    async (Guid id, TestAlertRequest? body, bool? persist, IAlertRuleRepository repo, AlertEngine engine, CancellationToken ct) =>
                    {
                        var rule = await repo.GetByIdAsync(id, ct).ConfigureAwait(false);
                        if (rule is null)
                            return Results.NotFound();

                        var trigger = new AlertTrigger
                        {
                            FlowId = body?.FlowId ?? Guid.Empty,
                            MessageId = body?.MessageId,
                            CorrelationId = body?.CorrelationId,
                            ErrorType = body?.ErrorType ?? AlertErrorType.Any,
                            ErrorDetail = body?.ErrorDetail ?? "synthetic test",
                            OccurredAtUtc = DateTimeOffset.UtcNow,
                        };
                        var outcomes = await engine.RunForTestAsync(rule, trigger, persist is true, ct).ConfigureAwait(false);
                        return Results.Ok(outcomes);
                    })
                .WithName("SmartConnect_TestAlertRule");

            var events = endpoints.MapGroup("/api/v1/admin/alert-events").WithTags("SmartConnect Admin");

            events.MapGet(
                    "/",
                    async (IAlertEventStore store, Guid? ruleId, int? take, CancellationToken ct) =>
                    {
                        var capped = take.GetValueOrDefault(50);
                        var list = ruleId.HasValue
                            ? await store.GetForRuleAsync(ruleId.Value, capped, ct).ConfigureAwait(false)
                            : await store.GetRecentAsync(capped, ct).ConfigureAwait(false);
                        return Results.Ok(list);
                    })
                .WithName("SmartConnect_ListAlertEvents");

            events.MapGet(
                    "/{id:guid}",
                    async (Guid id, IAlertEventStore store, CancellationToken ct) =>
                    {
                        var evt = await store.GetByIdAsync(id, ct).ConfigureAwait(false);
                        return evt is null ? Results.NotFound() : Results.Ok(evt);
                    })
                .WithName("SmartConnect_GetAlertEvent");

            return endpoints;
        }
    }

    private static AlertRule WithId(AlertRule body, Guid id) => new()
    {
        Id = id,
        Name = body.Name,
        Enabled = body.Enabled,
        Description = body.Description,
        EnabledFlowIds = body.EnabledFlowIds,
        ErrorPatterns = body.ErrorPatterns,
        Actions = body.Actions,
        ThrottleWindow = body.ThrottleWindow,
        Revision = body.Revision == 0 ? 1 : body.Revision,
        LastModifiedUtc = DateTimeOffset.UtcNow,
    };

    private sealed record TestAlertRequest
    {
        public TestAlertRequest(Guid? FlowId,
            Guid? MessageId,
            string? CorrelationId,
            AlertErrorType? ErrorType,
            string? ErrorDetail)
        {
            this.FlowId = FlowId;
            this.MessageId = MessageId;
            this.CorrelationId = CorrelationId;
            this.ErrorType = ErrorType;
            this.ErrorDetail = ErrorDetail;
        }
        public Guid? FlowId { get; init; }
        public Guid? MessageId { get; init; }
        public string? CorrelationId { get; init; }
        public AlertErrorType? ErrorType { get; init; }
        public string? ErrorDetail { get; init; }
        public void Deconstruct(out Guid? FlowId, out Guid? MessageId, out string? CorrelationId, out AlertErrorType? ErrorType, out string? ErrorDetail)
        {
            FlowId = this.FlowId;
            MessageId = this.MessageId;
            CorrelationId = this.CorrelationId;
            ErrorType = this.ErrorType;
            ErrorDetail = this.ErrorDetail;
        }
    }
}
