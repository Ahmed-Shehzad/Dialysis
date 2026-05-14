using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.BuildingBlocks.Intercessor;

internal sealed class RequestDispatcher<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors,
    IRequestHandler<TRequest, TResponse> handler) : IRequestDispatcher<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> DispatchAsync(TRequest request, CancellationToken cancellationToken)
    {
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
                throw new IntercessorValidationException(result.Error);
        }

        RequestHandlerDelegate<TResponse> next = () => handler.HandleAsync(request, cancellationToken);
        foreach (var behavior in behaviors.Reverse())
        {
            var captured = behavior;
            var previous = next;
            next = () => captured.HandleAsync(request, previous, cancellationToken);
        }

        return await next().ConfigureAwait(false);
    }
}
