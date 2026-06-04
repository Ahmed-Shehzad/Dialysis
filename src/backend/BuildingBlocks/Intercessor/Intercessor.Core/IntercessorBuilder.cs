using Dialysis.BuildingBlocks.Verifier;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Intercessor;

/// <summary>
/// Registers handlers, validators, and pipeline behaviors for Intercessor.
/// For each request type, call <see cref="AddHandler{TRequest,TResponse,THandler}"/> before
/// <see cref="AddBehavior{TRequest,TResponse,TBehavior}"/> so behaviors wrap the handler in registration order.
/// </summary>
public sealed class IntercessorBuilder
{
    private readonly IServiceCollection _services;
    /// <summary>
    /// Registers handlers, validators, and pipeline behaviors for Intercessor.
    /// For each request type, call <see cref="AddHandler{TRequest,TResponse,THandler}"/> before
    /// <see cref="AddBehavior{TRequest,TResponse,TBehavior}"/> so behaviors wrap the handler in registration order.
    /// </summary>
    public IntercessorBuilder(IServiceCollection services) => _services = services;
    /// <summary>
    /// Registers <typeparamref name="THandler"/> as the handler for <typeparamref name="TRequest"/>.
    /// </summary>
    public IntercessorBuilder AddHandler<TRequest, TResponse, THandler>()
        where TRequest : IRequest<TResponse>
        where THandler : class, IRequestHandler<TRequest, TResponse>
    {
        _services.AddScoped<IRequestHandler<TRequest, TResponse>, THandler>();
        _services.AddScoped<IRequestDispatcher<TRequest, TResponse>, RequestDispatcher<TRequest, TResponse>>();
        return this;
    }

    /// <summary>
    /// Registers an <see cref="IValidator{T}"/> for <typeparamref name="TRequest"/> (runs before pipeline behaviors).
    /// </summary>
    public IntercessorBuilder AddValidator<TRequest, TValidator>()
        where TValidator : class, IValidator<TRequest>
    {
        _services.TryAddEnumerable(ServiceDescriptor.Scoped<IValidator<TRequest>, TValidator>());
        return this;
    }

    /// <summary>
    /// Registers a pipeline behavior for <typeparamref name="TRequest"/>.
    /// </summary>
    public IntercessorBuilder AddBehavior<TRequest, TResponse, TBehavior>()
        where TRequest : IRequest<TResponse>
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
    {
        _services.TryAddEnumerable(ServiceDescriptor.Scoped<IPipelineBehavior<TRequest, TResponse>, TBehavior>());
        return this;
    }
}
