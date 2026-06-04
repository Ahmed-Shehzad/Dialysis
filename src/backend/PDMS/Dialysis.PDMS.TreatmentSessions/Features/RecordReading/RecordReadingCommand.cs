using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.RecordReading;

/// <summary>
/// Opted in to the durable command bus via <see cref="DurableCommandAttribute"/>: when the
/// PDMS host has the bus configured, the controller can route the write through the durable
/// path (202 + status endpoint) instead of synchronous dispatch. The handler is unchanged —
/// the durable consumer dispatches into the same <c>ICommandHandler</c> through the existing
/// CQRS gateway. <see cref="ReadingId"/> lets a retrying client supply a stable id so a
/// redelivery produces the same reading row; defaults to <see cref="Guid.Empty"/> for
/// in-process callers, in which case the handler generates a fresh id.
/// </summary>
[DurableCommand("pdms")]
public sealed record RecordReadingCommand : ICommand<Guid>, IPermissionedCommand
{
    /// <summary>
    /// Opted in to the durable command bus via <see cref="DurableCommandAttribute"/>: when the
    /// PDMS host has the bus configured, the controller can route the write through the durable
    /// path (202 + status endpoint) instead of synchronous dispatch. The handler is unchanged —
    /// the durable consumer dispatches into the same <c>ICommandHandler</c> through the existing
    /// CQRS gateway. <see cref="ReadingId"/> lets a retrying client supply a stable id so a
    /// redelivery produces the same reading row; defaults to <see cref="Guid.Empty"/> for
    /// in-process callers, in which case the handler generates a fresh id.
    /// </summary>
    public RecordReadingCommand(Guid SessionId,
        int SystolicBloodPressure,
        int DiastolicBloodPressure,
        int HeartRateBpm,
        decimal ArterialPressureMmHg,
        decimal VenousPressureMmHg,
        decimal UltrafiltrationRateMlPerHour,
        decimal ConductivityMsPerCm,
        string? Notes,
        Guid ReadingId = default)
    {
        this.SessionId = SessionId;
        this.SystolicBloodPressure = SystolicBloodPressure;
        this.DiastolicBloodPressure = DiastolicBloodPressure;
        this.HeartRateBpm = HeartRateBpm;
        this.ArterialPressureMmHg = ArterialPressureMmHg;
        this.VenousPressureMmHg = VenousPressureMmHg;
        this.UltrafiltrationRateMlPerHour = UltrafiltrationRateMlPerHour;
        this.ConductivityMsPerCm = ConductivityMsPerCm;
        this.Notes = Notes;
        this.ReadingId = ReadingId;
    }
    public string RequiredPermission => PdmsPermissions.ReadingRecord;
    public Guid SessionId { get; init; }
    public int SystolicBloodPressure { get; init; }
    public int DiastolicBloodPressure { get; init; }
    public int HeartRateBpm { get; init; }
    public decimal ArterialPressureMmHg { get; init; }
    public decimal VenousPressureMmHg { get; init; }
    public decimal UltrafiltrationRateMlPerHour { get; init; }
    public decimal ConductivityMsPerCm { get; init; }
    public string? Notes { get; init; }
    public Guid ReadingId { get; init; }
    public void Deconstruct(out Guid SessionId, out int SystolicBloodPressure, out int DiastolicBloodPressure, out int HeartRateBpm, out decimal ArterialPressureMmHg, out decimal VenousPressureMmHg, out decimal UltrafiltrationRateMlPerHour, out decimal ConductivityMsPerCm, out string? Notes, out Guid ReadingId)
    {
        SessionId = this.SessionId;
        SystolicBloodPressure = this.SystolicBloodPressure;
        DiastolicBloodPressure = this.DiastolicBloodPressure;
        HeartRateBpm = this.HeartRateBpm;
        ArterialPressureMmHg = this.ArterialPressureMmHg;
        VenousPressureMmHg = this.VenousPressureMmHg;
        UltrafiltrationRateMlPerHour = this.UltrafiltrationRateMlPerHour;
        ConductivityMsPerCm = this.ConductivityMsPerCm;
        Notes = this.Notes;
        ReadingId = this.ReadingId;
    }
}
