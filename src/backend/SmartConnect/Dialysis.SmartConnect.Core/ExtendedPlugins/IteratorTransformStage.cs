using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.Iteration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Wraps a child transform stage and runs it once per element produced by
/// <see cref="IterableResolver"/>. Mirth-equivalent of an Iterator Transformer Step
/// (User Guide pp 286, 288-294). Parameters JSON shape:
/// <code>
/// {
///   "iterableExpression": "OBX",
///   "child": { "kind": "javascript", "parametersJson": "{\"script\":\"...\"}" },
///   "iteratorVariableName": "elem",
///   "indexVariableName": "i",
///   "separator": "\r"
/// }
/// </code>
/// Output payload is the concatenation of every child-stage output, joined by <c>separator</c>
/// (default: empty string, i.e. raw concat). Existing payload is replaced.
/// </summary>
public sealed class IteratorTransformStage : ITransformStage
{
    public const string ParametersMetadataKey = "smartconnect.transform.parameters";
    public const string KindValue = "iterator";

    /// <summary>Metadata key set on each per-element sub-message — current element value.</summary>
    public const string ElementMetadataKey = "smartconnect.iterator.element";

    /// <summary>Metadata key set on each per-element sub-message — current element index (0-based).</summary>
    public const string IndexMetadataKey = "smartconnect.iterator.index";

    private readonly IServiceProvider _services;

    /// <summary>
    /// Takes <see cref="IServiceProvider"/> (not <see cref="IFlowPluginRegistry"/>) to avoid a DI cycle —
    /// the registry singleton's factory registers this stage, so the registry isn't yet built at construction time.
    /// We resolve the registry lazily on each evaluation.
    /// </summary>
    public IteratorTransformStage(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public string Kind => KindValue;

    public async Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(ParametersMetadataKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return message;
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var expression = root.TryGetProperty("iterableExpression", out var expr) ? expr.GetString() : null;
        if (string.IsNullOrWhiteSpace(expression))
            return message;

        if (!root.TryGetProperty("child", out var childEl) ||
            !childEl.TryGetProperty("kind", out var childKindEl))
            return message;
        var childKind = childKindEl.GetString();
        if (string.IsNullOrWhiteSpace(childKind))
            return message;
        var registry = _services.GetRequiredService<IFlowPluginRegistry>();
        var childStage = registry.TryResolveTransformStage(childKind);
        if (childStage is null)
            throw new InvalidOperationException($"Iterator child transform stage kind '{childKind}' is not registered.");

        var childParamsJson = childEl.TryGetProperty("parametersJson", out var pj)
            ? pj.GetString()
            : null;
        var separator = root.TryGetProperty("separator", out var sep) ? sep.GetString() ?? string.Empty : string.Empty;

        var elements = IterableResolver.Resolve(message, expression);
        if (elements.Count == 0)
        {
            return message;
        }

        var output = new StringBuilder();
        var first = true;
        foreach (var element in elements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subMessage = message
                .CloneWithPayload(Encoding.UTF8.GetBytes(element.Value), PayloadFormat.Utf8Text)
                .WithMetadata(ElementMetadataKey, element.Value)
                .WithMetadata(IndexMetadataKey, element.Index.ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(childParamsJson))
                subMessage = subMessage.WithMetadata(ParametersMetadataKey, childParamsJson);

            var transformed = await childStage.TransformAsync(subMessage, cancellationToken).ConfigureAwait(false);
            var transformedText = Encoding.UTF8.GetString(transformed.Payload.Span);

            if (!first)
                output.Append(separator);
            output.Append(transformedText);
            first = false;
        }

        return message.CloneWithPayload(Encoding.UTF8.GetBytes(output.ToString()), PayloadFormat.Utf8Text);
    }
}
