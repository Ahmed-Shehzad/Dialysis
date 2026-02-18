using Transponder.Abstractions;

namespace Dialysis.Treatment.Application.Contracts;

/// <summary>
/// Transponder message for real-time observation broadcast via SignalR.
/// </summary>
public sealed record ObservationRecordedMessage(
    string SessionId,
    string ObservationId,
    string Code,
    string? Value,
    string? Unit,
    string? SubId,
    string? ChannelName) : IMessage;

