namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// What an opt-in endpoint returns to the caller after a durable enqueue succeeds. The caller
/// either polls <see cref="StatusEndpoint"/> for completion or uses the deterministic
/// <see cref="CommandId"/> to compute the new aggregate's id where the slice supports the
/// "id-from-command-id" trick (PDMS RecordReading does — the reading's id IS the command id).
/// </summary>
public sealed record DurableCommandAcceptance
{
    /// <summary>
    /// What an opt-in endpoint returns to the caller after a durable enqueue succeeds. The caller
    /// either polls <see cref="StatusEndpoint"/> for completion or uses the deterministic
    /// <see cref="CommandId"/> to compute the new aggregate's id where the slice supports the
    /// "id-from-command-id" trick (PDMS RecordReading does — the reading's id IS the command id).
    /// </summary>
    public DurableCommandAcceptance(Guid CommandId,
        string CorrelationId,
        string StatusEndpoint)
    {
        this.CommandId = CommandId;
        this.CorrelationId = CorrelationId;
        this.StatusEndpoint = StatusEndpoint;
    }
    public Guid CommandId { get; init; }
    public string CorrelationId { get; init; }
    public string StatusEndpoint { get; init; }
    public void Deconstruct(out Guid CommandId, out string CorrelationId, out string StatusEndpoint)
    {
        CommandId = this.CommandId;
        CorrelationId = this.CorrelationId;
        StatusEndpoint = this.StatusEndpoint;
    }
}
