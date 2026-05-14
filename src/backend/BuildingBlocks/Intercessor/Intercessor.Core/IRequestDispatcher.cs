namespace Dialysis.BuildingBlocks.Intercessor;

internal interface IRequestDispatcher<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> DispatchAsync(TRequest request, CancellationToken cancellationToken);
}
