namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Optional publish metadata (correlation, etc.).
/// </summary>
/// <param name="CorrelationId">If null, transports may generate one for outbound messages.</param>
/// <param name="DeduplicationId">When set, used as broker message id / JetStream MsgId instead of defaulting to correlation (required for multi-part publishes such as <see cref="ITransponderBus.PublishLargeAsync{TMessage}"/>).</param>
public readonly record struct TransponderPublishOptions(string? CorrelationId = null, string? DeduplicationId = null);
