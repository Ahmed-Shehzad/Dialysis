namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// What an opt-in endpoint returns to the caller after a durable enqueue succeeds. The caller
/// either polls <see cref="StatusEndpoint"/> for completion or uses the deterministic
/// <see cref="CommandId"/> to compute the new aggregate's id where the slice supports the
/// "id-from-command-id" trick (PDMS RecordReading does — the reading's id IS the command id).
/// </summary>
public sealed record DurableCommandAcceptance(
    Guid CommandId,
    string CorrelationId,
    string StatusEndpoint);
