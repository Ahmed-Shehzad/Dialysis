using System.Reflection;

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

        object? handler = _serviceProvider.GetService(handlerType)
            ?? _serviceProvider.GetService(typeof(ICommandHandler<,>).MakeGenericType(requestType, responseType))
            ?? _serviceProvider.GetService(typeof(IQueryHandler<,>).MakeGenericType(requestType, responseType))
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        var behaviors = _serviceProvider
            .GetServices(typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType))
            .Cast<object>()
            .Reverse()
            .ToList();

        Func<Task<TResponse>> handlerFunc = () => InvokeHandlerAsync<TResponse>(handler!, request, cancellationToken);

        foreach (object? behavior in behaviors)
        {
            Func<Task<TResponse>> next = handlerFunc;
            handlerFunc = () => InvokeBehaviorAsync(behavior!, request, next, cancellationToken);
        }

        return await handlerFunc();
    }

    private async static Task<TResponse> InvokeHandlerAsync<TResponse>(object handler, IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        const string methodName = "HandleAsync";
        MethodInfo? method = handler.GetType().GetMethod(methodName, [request.GetType(), typeof(CancellationToken)]);
        if (method is null)
            throw new InvalidOperationException($"Handler {handler.GetType().Name} does not implement {methodName}(request, cancellationToken).");
        var task = (Task?)method.Invoke(handler, [request, cancellationToken]);
        if (task is null)
            throw new InvalidOperationException($"Handler {handler.GetType().Name}.{methodName} returned null.");
        await task;
        return ((dynamic)task).Result;
    }

    private async static Task<TResponse> InvokeBehaviorAsync<TResponse>(object behavior, IRequest<TResponse> request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
    {
        const string methodName = "HandleAsync";
        MethodInfo? method = behavior.GetType().GetMethod(methodName, [request.GetType(), typeof(Func<Task<TResponse>>), typeof(CancellationToken)]);
        if (method is null)
            throw new InvalidOperationException($"Behavior {behavior.GetType().Name} does not implement {methodName}(request, next, cancellationToken).");
        var task = (Task?)method.Invoke(behavior, [request, next, cancellationToken]);
        if (task is null)
            throw new InvalidOperationException($"Behavior {behavior.GetType().Name}.{methodName} returned null.");
        await task;
        return ((dynamic)task).Result;
    }

    public async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        Type requestType = request.GetType();
        Type handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);

        dynamic? handler = _serviceProvider.GetService(handlerType)
            ?? _serviceProvider.GetService(typeof(ICommandHandler<>).MakeGenericType(requestType))
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");
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
