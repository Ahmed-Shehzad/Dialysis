using System.Text.Json;
using Dialysis.SmartConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Maps <c>/smartconnect/v1/admin/events/*</c> query routes for audit log.</summary>
public static class EventsEndpointExtensions
{
    public static IEndpointRouteBuilder MapSmartConnectEventsRoutes(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/smartconnect/v1/admin/events").WithTags("SmartConnect Admin");

        group.MapGet(
                "/",
                async (
                    IAuditEventStore store,
                    int? category,
                    int? level,
                    Guid? flowId,
                    DateTimeOffset? from,
                    DateTimeOffset? to,
                    int? skip,
                    int? take,
                    CancellationToken ct) =>
                {
                    var events = await store.QueryAsync(
                        category.HasValue ? (AuditEventCategory)category.Value : null,
                        level.HasValue ? (AuditEventLevel)level.Value : null,
                        flowId,
                        from,
                        to,
                        skip ?? 0,
                        take ?? 50,
                        ct).ConfigureAwait(false);
                    return Results.Ok(events);
                })
            .WithName("SmartConnect_ListEvents");

        group.MapGet(
                "/export",
                async (
                    IAuditEventStore store,
                    int? category,
                    int? level,
                    Guid? flowId,
                    DateTimeOffset? from,
                    DateTimeOffset? to,
                    int? take,
                    CancellationToken ct) =>
                {
                    var cap = take is > 0 and <= 10_000 ? take.Value : 1_000;
                    var events = await store.QueryAsync(
                        category.HasValue ? (AuditEventCategory)category.Value : null,
                        level.HasValue ? (AuditEventLevel)level.Value : null,
                        flowId,
                        from,
                        to,
                        0,
                        cap,
                        ct).ConfigureAwait(false);
                    var json = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
                    return Results.Text(json, "application/json");
                })
            .WithName("SmartConnect_ExportEvents");

        group.MapGet(
                "/{eventId:guid}",
                async (Guid eventId, IAuditEventStore store, CancellationToken ct) =>
                {
                    var ev = await store.GetByIdAsync(eventId, ct).ConfigureAwait(false);
                    return ev is null ? Results.NotFound() : Results.Ok(ev);
                })
            .WithName("SmartConnect_GetEvent");

        return endpoints;
    }
}
