namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Marker for a request message that expects a response of <typeparamref name="TResponse"/>.
/// Request/response dispatch will be provided by the runtime (e.g. correlation-aware transport or in-memory test harness).
/// </summary>
public interface IRequestMessage<out TResponse> : IMessage
{
}
