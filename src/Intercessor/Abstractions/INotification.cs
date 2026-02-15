namespace Intercessor.Abstractions;

/// <summary>
/// Marker interface to represent an event or notification that can be published by the mediator.
/// Implementing this interface allows an object to be handled by one or more <see cref="INotificationHandler{TNotification}"/>.
/// </summary>
public interface INotification;