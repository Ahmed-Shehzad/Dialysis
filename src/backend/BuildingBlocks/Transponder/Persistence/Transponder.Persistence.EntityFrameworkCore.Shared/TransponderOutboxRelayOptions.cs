namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>Options for <see cref="TransponderOutboxRelayHostedService{TContext}"/>.</summary>
public sealed class TransponderOutboxRelayOptions
{
    /// <summary>Delay between polling cycles when no work remains.</summary>
    public TimeSpan IdlePollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Maximum outbox rows to load per cycle.</summary>
    public int BatchSize { get; set; } = 32;
}
