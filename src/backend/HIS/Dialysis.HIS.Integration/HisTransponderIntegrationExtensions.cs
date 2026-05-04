using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIS.Contracts.IntegrationEvents;

namespace Dialysis.HIS.Integration;

/// <summary>
/// Registers in-process <see cref="IConsumer{TMessage}"/> stubs for EHR, PDMS, pharmacy, and scheduling integration (checklist C4b / E3 / H2).
/// </summary>
public static class HisTransponderIntegrationExtensions
{
    public static TransponderBuilder AddHisIntegrationConsumers(this TransponderBuilder builder)
    {
        builder.AddConsumer<PatientAdmittedToHospitalIntegrationEvent, EhrPatientAdmittedStubConsumer>();
        builder.AddConsumer<PatientAdmittedToHospitalIntegrationEvent, PdmsPatientAdmittedStubConsumer>();
        builder.AddConsumer<PatientDischargedIntegrationEvent, EhrPatientDischargedStubConsumer>();
        builder.AddConsumer<ReferralCreatedIntegrationEvent, PdmsReferralCreatedStubConsumer>();
        builder.AddConsumer<ReferralCreatedIntegrationEvent, LaboratoryReferralFromHisStubConsumer>();
        builder.AddConsumer<AppointmentBookedIntegrationEvent, SchedulingIntegrationLogConsumer>();
        builder.AddConsumer<MedicationOrderPlacedIntegrationEvent, PharmacyMedicationOrderPlacedStubConsumer>();
        builder.AddConsumer<MedicationOrderDiscontinuedIntegrationEvent, PharmacyMedicationOrderDiscontinuedStubConsumer>();
        return builder;
    }
}
