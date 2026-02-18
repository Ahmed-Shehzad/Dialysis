using Intercessor.Abstractions;
using Intercessor.Behaviours;

using Microsoft.Extensions.DependencyInjection;

namespace Intercessor;

internal class Sender : ISender
{
    private readonly IServiceProvider _serviceProvider;

    public Sender(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        Type requestType = request.GetType();
        Type responseType = typeof(TResponse);
        Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);

        dynamic? handler = _serviceProvider.GetService(handlerType) ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");
        var behaviors = _serviceProvider
            .GetServices(typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType))
            .Cast<dynamic>()
            .Reverse()
            .ToList();

        Func<Task<TResponse>> handlerFunc = () => handler.HandleAsync((dynamic)request, cancellationToken);

        foreach (dynamic? behavior in behaviors)
        {
            Func<Task<TResponse>> next = handlerFunc;
            handlerFunc = () => behavior.HandleAsync((dynamic)request, next, cancellationToken);
        }

        TResponse response = await handlerFunc();

        return response;
    }

    public async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        Type requestType = request.GetType();
        Type handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);

        dynamic? handler = _serviceProvider.GetService(handlerType) ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");
        var behaviors = _serviceProvider
            .GetServices(typeof(IPipelineBehavior<>).MakeGenericType(requestType))
            .Cast<dynamic>()
            .Reverse()
            .ToList();

        Func<Task> handlerFunc = () => handler.HandleAsync((dynamic)request, cancellationToken);

        foreach (dynamic? behavior in behaviors)
        {
            Func<Task> next = handlerFunc;
            handlerFunc = () => behavior.HandleAsync((dynamic)request, next, cancellationToken);
        }

        await handlerFunc();
    }
}
