namespace Dialysis.DomainDrivenDesign.DomainEvents;

/// <summary>
/// Something meaningful that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC timestamp when the event was created.</summary>
    DateTime OccurredOn { get; }
}
