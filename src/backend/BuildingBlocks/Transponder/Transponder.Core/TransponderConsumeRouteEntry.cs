using Dialysis.BuildingBlocks.Transponder.Serialization;

namespace Dialysis.BuildingBlocks.Transponder;

internal sealed record TransponderConsumeRouteEntry
{
    public TransponderConsumeRouteEntry(Func<IMessageSerializer, ReadOnlyMemory<byte>, object?> Deserialize,
        Func<IServiceProvider, object, ITransponderBus, string?, string?, CancellationToken, Task> InvokeConsumers)
    {
        this.Deserialize = Deserialize;
        this.InvokeConsumers = InvokeConsumers;
    }
    public Func<IMessageSerializer, ReadOnlyMemory<byte>, object?> Deserialize { get; init; }
    public Func<IServiceProvider, object, ITransponderBus, string?, string?, CancellationToken, Task> InvokeConsumers { get; init; }
    public void Deconstruct(out Func<IMessageSerializer, ReadOnlyMemory<byte>, object?> Deserialize, out Func<IServiceProvider, object, ITransponderBus, string?, string?, CancellationToken, Task> InvokeConsumers)
    {
        Deserialize = this.Deserialize;
        InvokeConsumers = this.InvokeConsumers;
    }
}
