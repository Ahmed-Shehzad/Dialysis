namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips;

/// <summary>
/// Optional compensation for a routing slip activity. Invoked in reverse order of successful execution when a later activity faults.
/// </summary>
public interface IRoutingSlipCompensatableActivity : IRoutingSlipActivity
{
    Task CompensateAsync(IRoutingSlipActivityCompensationContext context, CancellationToken cancellationToken = default);
}
