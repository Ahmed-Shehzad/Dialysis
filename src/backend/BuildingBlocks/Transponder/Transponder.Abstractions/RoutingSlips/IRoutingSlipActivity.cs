namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// A named unit of work in a <see cref="TransponderRoutingSlipState"/> itinerary.
/// </summary>
public interface IRoutingSlipActivity
{
    string Name { get; }

    Task ExecuteAsync(IRoutingSlipActivityContext context, CancellationToken cancellationToken = default);
}
