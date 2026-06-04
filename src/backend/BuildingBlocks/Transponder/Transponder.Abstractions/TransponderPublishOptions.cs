namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Optional publish metadata (correlation, etc.).
/// </summary>
public readonly record struct TransponderPublishOptions
{
    /// <summary>
    /// Optional publish metadata (correlation, etc.).
    /// </summary>
    /// <param name="CorrelationId">If null, transports may generate one for outbound messages.</param>
    /// <param name="DeduplicationId">When set, used as broker message id / JetStream MsgId instead of defaulting to correlation (required for multi-part publishes such as <see cref="ITransponderBus.PublishLargeAsync{TMessage}"/>).</param>
    public TransponderPublishOptions(string? CorrelationId = null, string? DeduplicationId = null)
    {
        this.CorrelationId = CorrelationId;
        this.DeduplicationId = DeduplicationId;
    }

    /// <summary>If null, transports may generate one for outbound messages.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>When set, used as broker message id / JetStream MsgId instead of defaulting to correlation (required for multi-part publishes such as <see cref="ITransponderBus.PublishLargeAsync{TMessage}"/>).</summary>
    public string? DeduplicationId { get; init; }

    public void Deconstruct(out string? CorrelationId, out string? DeduplicationId)
    {
        CorrelationId = this.CorrelationId;
        DeduplicationId = this.DeduplicationId;
    }
}
