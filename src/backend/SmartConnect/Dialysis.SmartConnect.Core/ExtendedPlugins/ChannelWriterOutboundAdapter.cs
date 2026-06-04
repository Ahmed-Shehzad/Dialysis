using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Dialysis.SmartConnect.Inbound;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Mirth-equivalent Channel Writer: dispatches the current message into another flow on the same
/// SmartConnect host via the in-process <see cref="IInboundTransport"/>.
/// </summary>
/// <remarks>
/// Unlike Mirth's fire-and-forget Channel Writer, dispatch is awaited so the caller's outbound
/// route inherits the target flow's success/failure (including retry/backoff in
/// <see cref="FlowRuntimeEngine"/>). A loop guard tracks chain depth via metadata key
/// <see cref="DepthMetadataKey"/>.
/// </remarks>
public sealed class ChannelWriterOutboundAdapter : IOutboundAdapter
{
    private readonly IServiceScopeFactory _scopeFactory;
    /// <summary>
    /// Mirth-equivalent Channel Writer: dispatches the current message into another flow on the same
    /// SmartConnect host via the in-process <see cref="IInboundTransport"/>.
    /// </summary>
    /// <remarks>
    /// Unlike Mirth's fire-and-forget Channel Writer, dispatch is awaited so the caller's outbound
    /// route inherits the target flow's success/failure (including retry/backoff in
    /// <see cref="FlowRuntimeEngine"/>). A loop guard tracks chain depth via metadata key
    /// <see cref="DepthMetadataKey"/>.
    /// </remarks>
    public ChannelWriterOutboundAdapter(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;
    public const string KindValue = "channel-writer";

    public const string DepthMetadataKey = "smartconnect.channelwriter.depth";
    public const string SourceFlowIdMetadataKey = "smartconnect.channelwriter.sourceFlowId";
    public const string SourceMessageIdMetadataKey = "smartconnect.channelwriter.sourceMessageId";

    public string Kind => KindValue;

    public async Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(HttpOutboundAdapter.ParametersMetadataKey, out var json) ||
            string.IsNullOrWhiteSpace(json))
        {
            return new OutboundSendResult(
                false,
                "Channel writer outbound requires parameters JSON with TargetFlowId.");
        }

        ChannelWriterOutboundParameters? opts;
        try
        {
            opts = JsonSerializer.Deserialize<ChannelWriterOutboundParameters>(json);
        }
        catch (JsonException ex)
        {
            return new OutboundSendResult(false, $"Channel writer parameters JSON is invalid: {ex.Message}");
        }

        if (opts is null || opts.TargetFlowId == Guid.Empty)
        {
            return new OutboundSendResult(
                false,
                "Channel writer parameters must include a non-empty TargetFlowId.");
        }

        if (opts.TargetFlowId == message.FlowId)
        {
            return new OutboundSendResult(
                false,
                "Channel writer refused to dispatch to the same flow (self-loop).");
        }

        var currentDepth = 0;
        if (message.Metadata.TryGetValue(DepthMetadataKey, out var depthRaw)
            && int.TryParse(depthRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            currentDepth = parsed;
        }

        var maxDepth = opts.MaxDepth <= 0 ? 8 : opts.MaxDepth;
        if (currentDepth >= maxDepth)
        {
            return new OutboundSendResult(
                false,
                $"Channel writer depth limit ({maxDepth}) reached; aborting to prevent loop.");
        }

        var propagated = PropagateMetadata(message.Metadata, opts);
        propagated = propagated
            .SetItem(DepthMetadataKey, (currentDepth + 1).ToString(CultureInfo.InvariantCulture))
            .SetItem(SourceFlowIdMetadataKey, message.FlowId.ToString())
            .SetItem(SourceMessageIdMetadataKey, message.Id.ToString())
            // Strip outbound-route parameters; the next flow has its own pipeline.
            .Remove(HttpOutboundAdapter.ParametersMetadataKey);

        var nextMessage = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = opts.TargetFlowId,
            CorrelationId = opts.PreserveCorrelationId ? message.CorrelationId : Guid.CreateVersion7().ToString("N"),
            Payload = message.Payload,
            PayloadFormat = message.PayloadFormat,
            Metadata = propagated,
            ReceivedAtUtc = message.ReceivedAtUtc,
        };

        await using var scope = _scopeFactory.CreateAsyncScope();
        var transport = scope.ServiceProvider.GetRequiredService<IInboundTransport>();
        var result = await transport.DispatchAsync(nextMessage, cancellationToken).ConfigureAwait(false);

        return result.Succeeded
            ? new OutboundSendResult(true, null)
            : new OutboundSendResult(false, result.Error ?? "Channel writer downstream dispatch failed.");
    }

    private static ImmutableDictionary<string, string> PropagateMetadata(
        ImmutableDictionary<string, string> source,
        ChannelWriterOutboundParameters opts) =>
        opts.MetadataPropagation switch
        {
            ChannelWriterMetadataPropagation.All => source,
            ChannelWriterMetadataPropagation.None => ImmutableDictionary<string, string>.Empty,
            ChannelWriterMetadataPropagation.Whitelist => FilterByWhitelist(source, opts.MetadataKeys),
            _ => source,
        };

    private static ImmutableDictionary<string, string> FilterByWhitelist(
        ImmutableDictionary<string, string> source,
        IReadOnlyCollection<string> keys)
    {
        if (keys.Count == 0)
        {
            return ImmutableDictionary<string, string>.Empty;
        }

        var allowed = new HashSet<string>(keys, StringComparer.Ordinal);
        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var (k, v) in source)
        {
            if (allowed.Contains(k))
            {
                builder[k] = v;
            }
        }

        return builder.ToImmutable();
    }
}
