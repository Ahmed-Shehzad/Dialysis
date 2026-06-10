using System.Globalization;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.Iteration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Wraps a child route filter and evaluates it once per element produced by
/// <see cref="IterableResolver"/>. Mirth-equivalent of an Iterator Filter Rule (User Guide pp 280, 288-294).
/// Parameters JSON shape:
/// <code>
/// {
///   "iterableExpression": "OBX",
///   "child": { "kind": "rule-builder", "parametersJson": "{...}" },
///   "iteratorVariableName": "elem",
///   "indexVariableName": "i",
///   "minMatches": 1
/// }
/// </code>
/// Allow if at least <c>minMatches</c> child evaluations return Allow; otherwise Drop.
/// </summary>
public sealed class IteratorRouteFilter : IRouteFilter
{
    public const string ParametersMetadataKey = "smartconnect.filter.parameters";
    public const string KindValue = "iterator";

    /// <summary>Metadata key set on each per-element sub-message — current element value.</summary>
    public const string ElementMetadataKey = "smartconnect.iterator.element";

    /// <summary>Metadata key set on each per-element sub-message — current element index (0-based).</summary>
    public const string IndexMetadataKey = "smartconnect.iterator.index";

    private readonly IServiceProvider _services;

    /// <summary>
    /// Takes <see cref="IServiceProvider"/> (not <see cref="IFlowPluginRegistry"/>) to avoid a DI cycle —
    /// the registry singleton's factory registers this filter, so the registry isn't yet built at construction time.
    /// We resolve the registry lazily on each evaluation.
    /// </summary>
    public IteratorRouteFilter(IServiceProvider services) => _services = services ?? throw new ArgumentNullException(nameof(services));

    public string Kind => KindValue;

    public async Task<RouteFilterResult> EvaluateAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(ParametersMetadataKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return RouteFilterResult.Allow();
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var expression = root.TryGetProperty("iterableExpression", out var expr) ? expr.GetString() : null;
        if (string.IsNullOrWhiteSpace(expression))
            return RouteFilterResult.Allow();

        if (!root.TryGetProperty("child", out var childEl) ||
            !childEl.TryGetProperty("kind", out var childKindEl))
            return RouteFilterResult.Allow();
        var childKind = childKindEl.GetString();
        if (string.IsNullOrWhiteSpace(childKind))
            return RouteFilterResult.Allow();
        var registry = _services.GetRequiredService<IFlowPluginRegistry>();
        var childFilter = registry.TryResolveRouteFilter(childKind);
        if (childFilter is null)
            throw new InvalidOperationException($"Iterator child route filter kind '{childKind}' is not registered.");

        var childParamsJson = childEl.TryGetProperty("parametersJson", out var pj)
            ? pj.GetString()
            : null;
        var minMatches = root.TryGetProperty("minMatches", out var mm) && mm.TryGetInt32(out var mmi) && mmi > 0
            ? mmi
            : 1;

        var elements = IterableResolver.Resolve(message, expression);
        if (elements.Count == 0)
        {
            return RouteFilterResult.Drop();
        }

        var matched = 0;
        foreach (var element in elements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subMessage = message
                .CloneWithPayload(Encoding.UTF8.GetBytes(element.Value), PayloadFormat.Utf8Text)
                .WithMetadata(ElementMetadataKey, element.Value)
                .WithMetadata(IndexMetadataKey, element.Index.ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(childParamsJson))
                subMessage = subMessage.WithMetadata(ParametersMetadataKey, childParamsJson);

            var result = await childFilter.EvaluateAsync(subMessage, cancellationToken).ConfigureAwait(false);
            if (result.Disposition == RouteFilterDisposition.Allow)
            {
                matched++;
                if (matched >= minMatches)
                    return RouteFilterResult.Allow();
            }
        }

        return matched >= minMatches ? RouteFilterResult.Allow() : RouteFilterResult.Drop();
    }
}
