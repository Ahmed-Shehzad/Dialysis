using Dialysis.BuildingBlocks.Transponder.Serialization;

namespace Dialysis.BuildingBlocks.Transponder;

internal sealed record TransponderConsumeRouteEntry(
    Func<IMessageSerializer, ReadOnlyMemory<byte>, object?> Deserialize,
    Func<IServiceProvider, object, ITransponderBus, string?, string?, CancellationToken, Task> InvokeConsumers);
