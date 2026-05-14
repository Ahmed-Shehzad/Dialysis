using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Maps <c>/smartconnect/v1/ledger/*</c> query routes.</summary>
public static class LedgerEndpointExtensions
{
    public static IEndpointRouteBuilder MapSmartConnectLedgerRoutes(this IEndpointRouteBuilder endpoints)
    {
        var g = endpoints.MapGroup("/smartconnect/v1/ledger").WithTags("SmartConnect Ledger");

        g.MapGet(
                "/entries",
                async (
                    IMessageLedgerQuery query,
                    CancellationToken ct,
                    Guid? flowId = null,
                    string? correlationIdPrefix = null,
                    DateTimeOffset? fromUtc = null,
                    DateTimeOffset? toUtc = null,
                    int skip = 0,
                    int take = 50) =>
                {
                    var criteria = new MessageLedgerQueryCriteria
                    {
                        FlowId = flowId,
                        CorrelationIdPrefix = correlationIdPrefix,
                        CreatedFromUtc = fromUtc,
                        CreatedToUtc = toUtc,
                        Skip = skip,
                        Take = take <= 0 ? 50 : take,
                    };
                    var (items, total) = await query.QueryAsync(criteria, ct).ConfigureAwait(false);
                    return Results.Ok(new { total, items });
                })
            .WithName("SmartConnect_QueryLedger");

        return endpoints;
    }
}
