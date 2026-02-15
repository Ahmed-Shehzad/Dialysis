using Intercessor.Abstractions;

namespace BuildingBlocks.Abstractions;

public interface IEvent : INotification;

/// <summary>
/// Marker interface for domain events dispatched within the same bounded context.
/// </summary>
public interface IDomainEvent : IEvent;
