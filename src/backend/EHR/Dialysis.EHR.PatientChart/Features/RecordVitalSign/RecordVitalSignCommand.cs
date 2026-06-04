using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.RecordVitalSign;

/// <summary>
/// Vital-sign recording. Opted into the durable command bus (see
/// <c>docs/architecture/durable-writes.md</c>) — when
/// <c>Ehr:DurableCommands:RecordVitalSign:Enabled</c> is true the controller
/// returns 202 + a status URL instead of the synchronous 201. <see cref="ReadingId"/>
/// is the id-from-CommandId trick so a redelivery yields the same row and the 202
/// caller knows the id without polling.
/// </summary>
[DurableCommand("ehr")]
public sealed record RecordVitalSignCommand : ICommand<Guid>, IPermissionedCommand
{
    /// <summary>
    /// Vital-sign recording. Opted into the durable command bus (see
    /// <c>docs/architecture/durable-writes.md</c>) — when
    /// <c>Ehr:DurableCommands:RecordVitalSign:Enabled</c> is true the controller
    /// returns 202 + a status URL instead of the synchronous 201. <see cref="ReadingId"/>
    /// is the id-from-CommandId trick so a redelivery yields the same row and the 202
    /// caller knows the id without polling.
    /// </summary>
    public RecordVitalSignCommand(Guid PatientId,
        Guid? EncounterId,
        string LoincCode,
        string? Display,
        decimal Value,
        string UnitCode,
        DateTime ObservedAtUtc,
        Guid? RecordedByProviderId,
        Guid ReadingId = default)
    {
        this.PatientId = PatientId;
        this.EncounterId = EncounterId;
        this.LoincCode = LoincCode;
        this.Display = Display;
        this.Value = Value;
        this.UnitCode = UnitCode;
        this.ObservedAtUtc = ObservedAtUtc;
        this.RecordedByProviderId = RecordedByProviderId;
        this.ReadingId = ReadingId;
    }
    public string RequiredPermission => EhrPermissions.VitalsRecord;
    public Guid PatientId { get; init; }
    public Guid? EncounterId { get; init; }
    public string LoincCode { get; init; }
    public string? Display { get; init; }
    public decimal Value { get; init; }
    public string UnitCode { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public Guid? RecordedByProviderId { get; init; }
    public Guid ReadingId { get; init; }
    public void Deconstruct(out Guid PatientId, out Guid? EncounterId, out string LoincCode, out string? Display, out decimal Value, out string UnitCode, out DateTime ObservedAtUtc, out Guid? RecordedByProviderId, out Guid ReadingId)
    {
        PatientId = this.PatientId;
        EncounterId = this.EncounterId;
        LoincCode = this.LoincCode;
        Display = this.Display;
        Value = this.Value;
        UnitCode = this.UnitCode;
        ObservedAtUtc = this.ObservedAtUtc;
        RecordedByProviderId = this.RecordedByProviderId;
        ReadingId = this.ReadingId;
    }
}
