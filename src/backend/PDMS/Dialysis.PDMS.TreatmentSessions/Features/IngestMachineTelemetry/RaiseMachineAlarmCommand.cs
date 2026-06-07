using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;
using Dialysis.PDMS.TreatmentSessions.Adapters;
using Dialysis.PDMS.TreatmentSessions.Domain;

namespace Dialysis.PDMS.TreatmentSessions.Features.IngestMachineTelemetry;

/// <summary>
/// Direct HTTP ingest of a dialysis-machine alarm state-change (operator / device-gateway /
/// telemetry path), the synchronous twin of the SmartConnect <see cref="TreatmentAlarmConsumer"/>.
/// Funnels through the shared <see cref="TreatmentAlarmIngestionService"/> so a machine alarm
/// behaves identically whether it arrives over RabbitMQ or over HTTP. <paramref name="State"/>
/// is the wire state token: <c>Present</c> | <c>Inactivating</c> | <c>Resolved</c>.
/// </summary>
public sealed record RaiseMachineAlarmCommand(
    string MachineSerial,
    long AlarmCode,
    string? AlarmSource,
    string? AlarmPhase,
    string State,
    DateTime ObservedAtUtc,
    Guid? SessionId)
    : ICommand<Unit>, IPermissionedCommand
{
    /// <summary>Telemetry-shape write — gated on the reading-record permission like other device ingest.</summary>
    public string RequiredPermission => PdmsPermissions.ReadingRecord;
}

/// <summary>Handles <see cref="RaiseMachineAlarmCommand"/> by translating to an
/// <see cref="IncomingAlarm"/> and applying it through the shared ingestion pipeline.</summary>
public sealed class RaiseMachineAlarmCommandHandler : ICommandHandler<RaiseMachineAlarmCommand, Unit>
{
    private readonly TreatmentAlarmIngestionService _ingestion;

    /// <summary>Creates the handler.</summary>
    public RaiseMachineAlarmCommandHandler(TreatmentAlarmIngestionService ingestion) => _ingestion = ingestion;

    /// <inheritdoc />
    public async Task<Unit> HandleAsync(RaiseMachineAlarmCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MachineSerial))
            throw new DomainException("Machine serial required.");
        if (!Enum.TryParse<TreatmentAlarmState>(request.State, ignoreCase: true, out var state))
            throw new DomainException($"Unknown alarm state '{request.State}'.");

        var incoming = new IncomingAlarm(
            MachineSerial: request.MachineSerial,
            AlarmCode: request.AlarmCode,
            AlarmSource: request.AlarmSource,
            AlarmPhase: request.AlarmPhase,
            State: state,
            ObservedAtUtc: request.ObservedAtUtc == default ? DateTime.UtcNow : request.ObservedAtUtc,
            MessageControlId: Guid.CreateVersion7().ToString("N"));

        await _ingestion.ApplyAsync(incoming, request.SessionId, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
