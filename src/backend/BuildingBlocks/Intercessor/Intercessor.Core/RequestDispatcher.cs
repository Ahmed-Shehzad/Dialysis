using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.BuildingBlocks.Intercessor;

internal sealed class RequestDispatcher<TRequest, TResponse> : IRequestDispatcher<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly IEnumerable<IPipelineBehavior<TRequest, TResponse>> _behaviors;
    private readonly IRequestHandler<TRequest, TResponse> _handler;
    public RequestDispatcher(IEnumerable<IValidator<TRequest>> validators,
        IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors,
        IRequestHandler<TRequest, TResponse> handler)
    {
        _validators = validators;
        _behaviors = behaviors;
        _handler = handler;
    }
    public async Task<TResponse> DispatchAsync(TRequest request, CancellationToken cancellationToken)
    {
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
                throw new IntercessorValidationException(result.Error);
        }

        RequestHandlerDelegate<TResponse> next = () => _handler.HandleAsync(request, cancellationToken);
        foreach (var behavior in _behaviors.Reverse())
        {
            var captured = behavior;
            var previous = next;
            next = () => captured.HandleAsync(request, previous, cancellationToken);
        }

        return await next().ConfigureAwait(false);
    }
}
