using Transponder.Abstractions;

namespace BuildingBlocks.Abstractions;

/// <summary>
/// Marker interface for integration events published outside the bounded context.
/// </summary>
public interface IIntegrationEvent : IEvent, ICorrelatedMessage;
