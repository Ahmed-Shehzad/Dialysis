using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Intercessor;

internal sealed class Intercessor : IIntercessor
{
    private readonly IServiceProvider _services;
    public Intercessor(IServiceProvider services) => _services = services;
    public Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);
        return _services.GetRequiredService<IRequestDispatcher<TRequest, TResponse>>()
            .DispatchAsync(request, cancellationToken);
    }
}
