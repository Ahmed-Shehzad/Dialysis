using System.Text.Json;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.Endpoints;

/// <summary>
/// Default <see cref="IEndpointResolver"/>. When the input parses as
/// <c>{"endpointRef":"name"}</c> the resolver looks up the named endpoint via
/// <see cref="IEndpointRepository"/> and returns its stored <c>ParametersJson</c>; otherwise it
/// returns the input unchanged.
///
/// Opens a fresh DI scope per resolve so it is safe to call concurrently from the engine's
/// parallel outbound dispatch (per-route DI scope pattern PR #92 introduced for alerts and
/// the routing slice extended to ledger writes).
/// </summary>
public sealed class DefaultEndpointResolver(IServiceScopeFactory scopeFactory, ILogger<DefaultEndpointResolver> logger) : IEndpointResolver
{
    public async Task<string?> ResolveParametersJsonAsync(string? nameOrInline, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nameOrInline))
        {
            return nameOrInline;
        }

        string? endpointName = null;
        try
        {
            using var doc = JsonDocument.Parse(nameOrInline);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("endpointRef", out var refEl) &&
                refEl.ValueKind == JsonValueKind.String)
            {
                endpointName = refEl.GetString();
            }
        }
        catch (JsonException)
        {
            // Not JSON — pass through unchanged so non-JSON parameter strings still work.
            return nameOrInline;
        }

        if (string.IsNullOrWhiteSpace(endpointName))
        {
            return nameOrInline;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetService<IEndpointRepository>();
        if (repo is null)
        {
            logger.LogWarning(
                "Route references endpoint '{Name}' but IEndpointRepository is not registered; passing through inline JSON.",
                endpointName);
            return nameOrInline;
        }

        var endpoint = await repo.GetByNameAsync(endpointName, cancellationToken).ConfigureAwait(false);
        if (endpoint is null)
        {
            logger.LogWarning(
                "Route references endpoint '{Name}' but no row is registered; the adapter will fail to resolve its parameters.",
                endpointName);
            return null;
        }

        return endpoint.ParametersJson;
    }
}
