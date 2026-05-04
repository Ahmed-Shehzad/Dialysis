using Xunit;

namespace Dialysis.BuildingBlocks.Transponder.Tests;

/// <summary>Serializes routing slip tests that share static <see cref="RoutingSlipEventTests"/> event logs.</summary>
[CollectionDefinition(nameof(RoutingSlipTestCollection), DisableParallelization = true)]
public sealed class RoutingSlipTestCollection
{
}
