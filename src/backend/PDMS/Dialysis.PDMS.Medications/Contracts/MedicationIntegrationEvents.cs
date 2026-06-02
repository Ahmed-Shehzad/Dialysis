using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.PDMS.Medications.Contracts;

/// <summary>Published when a clinician records a positive medication administration.</summary>
public sealed class MedicationAdministeredIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public int SchemaVersion { get; init; } = 1;
    public required Guid EntryId { get; init; }
    public required Guid SessionId { get; init; }
    public required Guid PatientId { get; init; }
    public required string MedicationCodeSystem { get; init; }
    public required string MedicationCode { get; init; }
    public required string MedicationDisplay { get; init; }
    public required decimal DoseQuantity { get; init; }
    public required string DoseUnit { get; init; }
    public required string Route { get; init; }
    public required DateTime AdministeredAtUtc { get; init; }
    public required string AdministeredBySub { get; init; }
    public Guid? RelatedOrderId { get; init; }
}

/// <summary>Published when a clinician records a declined dose with the operator reason.</summary>
public sealed class MedicationDeclinedIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public int SchemaVersion { get; init; } = 1;
    public required Guid EntryId { get; init; }
    public required Guid SessionId { get; init; }
    public required Guid PatientId { get; init; }
    public required string MedicationCodeSystem { get; init; }
    public required string MedicationCode { get; init; }
    public required DateTime DeclinedAtUtc { get; init; }
    public required string DeclinedBySub { get; init; }
    public required string Reason { get; init; }
    public Guid? RelatedOrderId { get; init; }
}

/// <summary>Published when an IV pump infusion starts (Status: Running).</summary>
public sealed class IvPumpInfusionStartedIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public int SchemaVersion { get; init; } = 1;
    public required Guid InfusionId { get; init; }
    public required Guid SessionId { get; init; }
    public required string PumpDeviceId { get; init; }
    public required string VendorCode { get; init; }
    public string? MedicationCodeSystem { get; init; }
    public string? MedicationCode { get; init; }
    public required decimal ProgrammedRateMlPerHour { get; init; }
    public required decimal ProgrammedVolumeMl { get; init; }
    public required DateTime StartedAtUtc { get; init; }
}

/// <summary>Published when an IV pump infusion ends successfully.</summary>
public sealed class IvPumpInfusionCompletedIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public int SchemaVersion { get; init; } = 1;
    public required Guid InfusionId { get; init; }
    public required Guid SessionId { get; init; }
    public required decimal InfusedVolumeMl { get; init; }
    public required DateTime EndedAtUtc { get; init; }
}

/// <summary>Published when an IV pump raises an alarm; consumed by the on-call notification pipeline.</summary>
public sealed class IvPumpAlarmRaisedIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public int SchemaVersion { get; init; } = 1;
    public required Guid InfusionId { get; init; }
    public required Guid SessionId { get; init; }
    public required Guid ChairId { get; init; }
    public required string PumpDeviceId { get; init; }
    public required string AlarmCode { get; init; }
    public required string AlarmText { get; init; }
    public required IvPumpAlarmSeverity Severity { get; init; }
    public required DateTime RaisedAtUtc { get; init; }
}

public enum IvPumpAlarmSeverity
{
    Informational = 0,
    Warning = 1,
    Critical = 2,
}

/// <summary>Published when an inventory item's on-hand count crosses below its threshold.</summary>
public sealed class MedicationInventoryLowIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public int SchemaVersion { get; init; } = 1;
    public required Guid InventoryItemId { get; init; }
    public required string MedicationCodeSystem { get; init; }
    public required string MedicationCode { get; init; }
    public required string MedicationDisplay { get; init; }
    public required string LotNumber { get; init; }
    public required int OnHandUnits { get; init; }
    public required int Threshold { get; init; }
}
