namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Serialized snapshot of a message to publish when a schedule fires. Used by scheduler integrations (Hangfire, Quartz, …).
/// </summary>
public sealed class TransponderScheduledEnvelope
{
    /// <summary>Assembly-qualified CLR type name of the message contract.</summary>
    public required string AssemblyQualifiedMessageTypeName { get; init; }

    /// <summary>UTF-8 JSON body produced by <see cref="IMessageSerializer"/>.</summary>
    public required string JsonPayload { get; init; }

    public string? CorrelationId { get; init; }

    public string? DeduplicationId { get; init; }
}
