using System.Reflection;
using Dialysis.BuildingBlocks.Intercessor;
using Dialysis.BuildingBlocks.Verifier;
using Dialysis.CQRS.Commands;
using Dialysis.CQRS.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.CQRS;

/// <summary>
/// Registers CQRS handlers, validators, and pipeline behaviors via Intercessor.
/// </summary>
public sealed class CqrsBuilder
{
    private readonly IntercessorBuilder _inner;
    private readonly IServiceCollection _services;
    /// <summary>
    /// Registers CQRS handlers, validators, and pipeline behaviors via Intercessor.
    /// </summary>
    public CqrsBuilder(IntercessorBuilder inner, IServiceCollection services)
    {
        _inner = inner;
        _services = services;
    }
    /// <summary>
    /// Registers a query handler.
    /// </summary>
    public CqrsBuilder AddQueryHandler<TQuery, TResponse, THandler>()
        where TQuery : IQuery<TResponse>
        where THandler : class, IRequestHandler<TQuery, TResponse>
    {
        _inner.AddHandler<TQuery, TResponse, THandler>();
        return this;
    }

    /// <summary>
    /// Registers a command handler.
    /// </summary>
    public CqrsBuilder AddCommandHandler<TCommand, TResponse, THandler>()
        where TCommand : ICommand<TResponse>
        where THandler : class, IRequestHandler<TCommand, TResponse>
    {
        _inner.AddHandler<TCommand, TResponse, THandler>();
        return this;
    }

    /// <summary>
    /// Registers a handler for a non-generic <see cref="ICommand"/> (returns <see cref="Unit"/>).
    /// </summary>
    public CqrsBuilder AddCommandHandler<TCommand, THandler>()
        where TCommand : ICommand
        where THandler : class, IRequestHandler<TCommand, Unit>
    {
        _inner.AddHandler<TCommand, Unit, THandler>();
        return this;
    }

    /// <summary>
    /// Registers a validator for a query type (runs before pipeline behaviors).
    /// </summary>
    public CqrsBuilder AddQueryValidator<TQuery, TValidator>()
        where TValidator : class, IValidator<TQuery>
    {
        _inner.AddValidator<TQuery, TValidator>();
        return this;
    }

    /// <summary>
    /// Registers a validator for a command type (runs before pipeline behaviors).
    /// </summary>
    public CqrsBuilder AddCommandValidator<TCommand, TValidator>()
        where TValidator : class, IValidator<TCommand>
    {
        _inner.AddValidator<TCommand, TValidator>();
        return this;
    }

    /// <summary>
    /// Registers a pipeline behavior for a query type.
    /// </summary>
    public CqrsBuilder AddQueryBehavior<TQuery, TResponse, TBehavior>()
        where TQuery : IQuery<TResponse>
        where TBehavior : class, IPipelineBehavior<TQuery, TResponse>
    {
        _inner.AddBehavior<TQuery, TResponse, TBehavior>();
        return this;
    }

    /// <summary>
    /// Registers a pipeline behavior for a command type.
    /// </summary>
    public CqrsBuilder AddCommandBehavior<TCommand, TResponse, TBehavior>()
        where TCommand : ICommand<TResponse>
        where TBehavior : class, IPipelineBehavior<TCommand, TResponse>
    {
        _inner.AddBehavior<TCommand, TResponse, TBehavior>();
        return this;
    }

    /// <summary>
    /// Registers concrete <see cref="Queries.IQueryHandler{TQuery,TResponse}"/> and
    /// <see cref="Commands.ICommandHandler{TCommand,TResponse}"/> types, and CQRS-scoped
    /// <see cref="IValidator{T}"/> implementations, discovered in the given assemblies.
    /// Validators are registered with Scrutor (<c>services.Scan</c> and <c>RegistrationStrategy.Append</c>);
    /// handlers use Intercessor's <see cref="IntercessorBuilder.AddHandler{TRequest,TResponse,THandler}"/> so dispatchers are wired.
    /// </summary>
    public CqrsBuilder AddFromAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        CqrsAssemblyScanner.Register(_inner, _services, assemblies);
        return this;
    }

    /// <summary>
    /// Same as <see cref="AddFromAssemblies"/> but resolves assemblies from marker types (distinct assemblies only).
    /// </summary>
    public CqrsBuilder AddFromAssembliesOf(params Type[] markerTypes)
    {
        ArgumentNullException.ThrowIfNull(markerTypes);
        if (markerTypes.Length == 0)
            return this;

        return AddFromAssemblies([.. markerTypes.Where(static t => t is not null).Select(static t => t.Assembly).Distinct()]);
    }
}
