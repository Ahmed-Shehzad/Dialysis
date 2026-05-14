using Dialysis.BuildingBlocks.Transponder.Sagas;

namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips;

/// <summary>
/// Marker for durable routing slip rows stored in <see cref="ITransponderSagaStore"/> / <c>SagaInstances</c>.
/// <see cref="SagaKind"/> is used as <see cref="TransponderSagaRecord.SagaKind"/>; <see cref="TransponderSagaRecord.InstanceKey"/> is the slip id.
/// </summary>
public static class TransponderRoutingSlipPersistenceKind
{
    /// <summary>Stable saga family id shared by all routing slip instances.</summary>
    public static string SagaKind { get; } = typeof(TransponderRoutingSlipPersistenceKind).FullName!;
}
