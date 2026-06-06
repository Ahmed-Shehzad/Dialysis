using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Dialysis.HIS.PatientFlow.Domain.ValueObjects;

namespace Dialysis.HIS.PatientFlow.Domain;

public sealed class Admission : AggregateRoot<Guid>
{
    public Guid PatientId { get; private set; }
    public WardCode Ward { get; private set; } = null!;
    public DateTime AdmittedAtUtc { get; private set; }
    public DateTime? DischargedAtUtc { get; private set; }

    private Admission() { }
    private Admission(Guid id) : base(id) { }

    public static Admission Admit(Guid patientId, WardCode ward, DateTime nowUtc)
    {
        if (patientId == Guid.Empty) throw new DomainException("PatientId cannot be empty.");
        ArgumentNullException.ThrowIfNull(ward);

        var admission = new Admission(Guid.CreateVersion7())
        {
            PatientId = patientId,
            Ward = ward,
            AdmittedAtUtc = nowUtc,
        };

        admission.RaiseIntegrationEvent(new PatientAdmittedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: nowUtc,
            SchemaVersion: 1,
            AdmissionId: admission.Id,
            PatientId: patientId,
            WardCode: ward.Value,
            AdmittedAtUtc: nowUtc));

        return admission;
    }

    public void Discharge(DateTime nowUtc)
    {
        if (DischargedAtUtc is not null) throw new DomainException("Admission already discharged.");
        DischargedAtUtc = nowUtc;

        RaiseIntegrationEvent(new PatientDischargedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: nowUtc,
            SchemaVersion: 1,
            AdmissionId: Id,
            PatientId: PatientId,
            WardCode: Ward.Value,
            DischargedAtUtc: nowUtc));
    }
}
