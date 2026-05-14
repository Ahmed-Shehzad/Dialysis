namespace Dialysis.DomainDrivenDesign.IntegrationEvents;

/// <summary>
/// A contract-first message published <strong>outside</strong> the bounded context (e.g. to a bus),
/// after the local domain transaction commits. Distinct from in-bounded-context <see cref="Dialysis.DomainDrivenDesign.DomainEvents.IDomainEvent"/>.
/// </summary>
/// <remarks>
/// Per Evans pp. 263–264 (Open Host Service / Published Language) each integration event is a versioned,
/// stable contract for cross-context consumers. Breaking changes increment <see cref="SchemaVersion"/> and
/// rename the type with a <c>V&lt;n&gt;</c> suffix; consumers re-subscribe to the new type. See each module's
/// <c>Contracts/Integration*/Versioning.md</c> for the policy.
/// </remarks>
public interface IIntegrationEvent
{
    /// <summary>Stable id for deduplication and tracing.</summary>
    Guid EventId { get; }

    /// <summary>UTC time when the integration event was created (usually at publish boundary).</summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Schema version of the event contract. Increments on any breaking change to the payload shape
    /// (rename, retype, semantic change). Combined with the type name forms the wire identity.
    /// </summary>
    int SchemaVersion { get; }
}
