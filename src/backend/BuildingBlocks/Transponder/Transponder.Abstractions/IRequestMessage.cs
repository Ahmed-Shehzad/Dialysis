namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Marker for a request message that expects a response of <typeparamref name="TResponse"/>.
/// Request/response dispatch will be provided by the runtime (e.g. correlation-aware transport or in-memory test harness).
/// </summary>
// TResponse is a phantom type parameter: correlation-aware dispatch infers the response
// type from the message alone — same marker pattern as Intercessor's IRequest<TResponse>.
#pragma warning disable S2326
public interface IRequestMessage<out TResponse> : IMessage
{
}
#pragma warning restore S2326
