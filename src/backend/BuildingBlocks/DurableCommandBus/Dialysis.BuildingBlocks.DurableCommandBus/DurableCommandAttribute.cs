namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Marks an <see cref="Dialysis.CQRS.Commands.ICommand{TResponse}"/> as routable through the
/// durable command bus. The attribute is documentation + a discovery hook; opting a command
/// in still requires calling <c>RegisterCommand&lt;TCommand,TResult&gt;</c> on the durable-bus
/// builder, since that's where the typed dispatcher closure is built.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class DurableCommandAttribute : Attribute
{
    /// <summary>
    /// Marks an <see cref="Dialysis.CQRS.Commands.ICommand{TResponse}"/> as routable through the
    /// durable command bus. The attribute is documentation + a discovery hook; opting a command
    /// in still requires calling <c>RegisterCommand&lt;TCommand,TResult&gt;</c> on the durable-bus
    /// builder, since that's where the typed dispatcher closure is built.
    /// </summary>
    public DurableCommandAttribute(string moduleSlug) => ModuleSlug = moduleSlug;

    /// <summary>The owning module's slug (e.g. <c>"pdms"</c>). Records which module owns the queue.</summary>
    public string ModuleSlug { get; }
}
