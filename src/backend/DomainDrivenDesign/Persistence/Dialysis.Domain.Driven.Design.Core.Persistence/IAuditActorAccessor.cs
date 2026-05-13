namespace Dialysis.DomainDrivenDesign.Persistence;

/// <summary>
/// Resolves the actor identifier stamped onto <see cref="Primitives.Audit"/> fields during SaveChanges.
/// Modules supply their own implementation (e.g. backed by <c>HttpContext</c>, message envelope, or a system actor).
/// </summary>
public interface IAuditActorAccessor
{
    string? ActorId { get; }
}

internal sealed class NullAuditActorAccessor : IAuditActorAccessor
{
    public string? ActorId => null;
}
