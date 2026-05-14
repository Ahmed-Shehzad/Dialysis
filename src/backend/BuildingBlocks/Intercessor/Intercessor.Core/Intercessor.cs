using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Intercessor;

internal sealed class Intercessor(IServiceProvider services) : IIntercessor
{
    public Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);
        return services.GetRequiredService<IRequestDispatcher<TRequest, TResponse>>()
            .DispatchAsync(request, cancellationToken);
    }
}
