namespace Dialysis.SmartConnect.ExtendedPlugins;

public sealed class ChannelWriterOutboundParameters
{
    public Guid TargetFlowId { get; set; }

    public bool PreserveCorrelationId { get; set; } = true;

    public ChannelWriterMetadataPropagation MetadataPropagation { get; set; } = ChannelWriterMetadataPropagation.All;

    public List<string> MetadataKeys { get; set; } = [];

    /// <summary>Maximum nesting depth before the chain is stopped (default 8).</summary>
    public int MaxDepth { get; set; } = 8;
}

public enum ChannelWriterMetadataPropagation
{
    All = 0,
    None = 1,
    Whitelist = 2,
}
