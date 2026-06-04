using System.Text.RegularExpressions;
using Dialysis.SmartConnect.Inbound;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Routing;

/// <summary>
/// Default <see cref="IMessageRouter"/>. Loads all Started flows on demand and evaluates each
/// flow's <c>InboundSubscriptions</c> against the candidate. Returns flow ids whose subscriptions
/// match. Opens a fresh DI scope per call so concurrent dispatches from many source connectors
/// don't race the engine's scoped DbContext.
/// </summary>
public sealed class DefaultMessageRouter : IMessageRouter
{
    private readonly IServiceScopeFactory _scopeFactory;
    /// <summary>
    /// Default <see cref="IMessageRouter"/>. Loads all Started flows on demand and evaluates each
    /// flow's <c>InboundSubscriptions</c> against the candidate. Returns flow ids whose subscriptions
    /// match. Opens a fresh DI scope per call so concurrent dispatches from many source connectors
    /// don't race the engine's scoped DbContext.
    /// </summary>
    public DefaultMessageRouter(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;
    public async Task<IReadOnlyList<Guid>> ResolveFlowIdsAsync(MessageRoutingCandidate candidate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetService<SmartConnectDbContext>();
        if (db is null)
        {
            return [];
        }

        var startedRaw = await db.IntegrationFlows
            .AsNoTracking()
            .Where(f => f.RuntimeState == (int)FlowRuntimeState.Started)
            .Select(f => new { f.Id, f.PipelineJson })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var matched = new List<Guid>();
        foreach (var row in startedRaw)
        {
            IntegrationFlowPipelineDefinition pipeline;
            try
            {
                pipeline = PipelineJsonSerializer.Deserialize(row.PipelineJson);
            }
            catch
            {
                continue;
            }

            if (pipeline.InboundSubscriptions.Count == 0)
            {
                continue;
            }

            foreach (var sub in pipeline.InboundSubscriptions)
            {
                if (Matches(sub, candidate))
                {
                    matched.Add(row.Id);
                    break;
                }
            }
        }

        return matched;
    }

    internal static bool Matches(InboundSubscriptionSlot sub, MessageRoutingCandidate c)
    {
        if (!string.IsNullOrWhiteSpace(sub.SourceKind)
            && !string.Equals(sub.SourceKind, c.SourceKind, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(sub.SenderId)
            && !string.Equals(sub.SenderId, c.SenderId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(sub.MessageTypePattern))
        {
            if (string.IsNullOrEmpty(c.MessageType))
            {
                return false;
            }
            var rx = GlobToRegex(sub.MessageTypePattern);
            if (!rx.IsMatch(c.MessageType))
            {
                return false;
            }
        }

        return true;
    }

    private static Regex GlobToRegex(string pattern)
    {
        var escaped = Regex.Escape(pattern)
            .Replace("\\*", ".*", StringComparison.Ordinal)
            .Replace("\\?", ".", StringComparison.Ordinal);
        return new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
