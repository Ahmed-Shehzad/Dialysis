using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

public sealed record AppointmentBookedIntegrationEvent : IIntegrationEvent
{
    public AppointmentBookedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid AppointmentId,
        Guid PatientId,
        Guid ProviderId,
        DateTime StartUtc,
        DateTime EndUtc,
        string EncounterClassCode,
        string? VisitReason)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.AppointmentId = AppointmentId;
        this.PatientId = PatientId;
        this.ProviderId = ProviderId;
        this.StartUtc = StartUtc;
        this.EndUtc = EndUtc;
        this.EncounterClassCode = EncounterClassCode;
        this.VisitReason = VisitReason;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid AppointmentId { get; init; }
    public Guid PatientId { get; init; }
    public Guid ProviderId { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public string EncounterClassCode { get; init; }
    public string? VisitReason { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid AppointmentId, out Guid PatientId, out Guid ProviderId, out DateTime StartUtc, out DateTime EndUtc, out string EncounterClassCode, out string? VisitReason)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        AppointmentId = this.AppointmentId;
        PatientId = this.PatientId;
        ProviderId = this.ProviderId;
        StartUtc = this.StartUtc;
        EndUtc = this.EndUtc;
        EncounterClassCode = this.EncounterClassCode;
        VisitReason = this.VisitReason;
    }
}

public sealed record AppointmentCancelledIntegrationEvent : IIntegrationEvent
{
    public AppointmentCancelledIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid AppointmentId,
        Guid PatientId,
        string ReasonCode)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.AppointmentId = AppointmentId;
        this.PatientId = PatientId;
        this.ReasonCode = ReasonCode;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid AppointmentId { get; init; }
    public Guid PatientId { get; init; }
    public string ReasonCode { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid AppointmentId, out Guid PatientId, out string ReasonCode)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        AppointmentId = this.AppointmentId;
        PatientId = this.PatientId;
        ReasonCode = this.ReasonCode;
    }
}

public sealed record AppointmentRescheduledIntegrationEvent : IIntegrationEvent
{
    public AppointmentRescheduledIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid AppointmentId,
        Guid PatientId,
        DateTime NewStartUtc,
        DateTime NewEndUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.AppointmentId = AppointmentId;
        this.PatientId = PatientId;
        this.NewStartUtc = NewStartUtc;
        this.NewEndUtc = NewEndUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid AppointmentId { get; init; }
    public Guid PatientId { get; init; }
    public DateTime NewStartUtc { get; init; }
    public DateTime NewEndUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid AppointmentId, out Guid PatientId, out DateTime NewStartUtc, out DateTime NewEndUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        AppointmentId = this.AppointmentId;
        PatientId = this.PatientId;
        NewStartUtc = this.NewStartUtc;
        NewEndUtc = this.NewEndUtc;
    }
}

public sealed record AppointmentCheckedInIntegrationEvent : IIntegrationEvent
{
    public AppointmentCheckedInIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid AppointmentId,
        Guid PatientId,
        DateTime CheckedInAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.AppointmentId = AppointmentId;
        this.PatientId = PatientId;
        this.CheckedInAtUtc = CheckedInAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid AppointmentId { get; init; }
    public Guid PatientId { get; init; }
    public DateTime CheckedInAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid AppointmentId, out Guid PatientId, out DateTime CheckedInAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        AppointmentId = this.AppointmentId;
        PatientId = this.PatientId;
        CheckedInAtUtc = this.CheckedInAtUtc;
    }
}
