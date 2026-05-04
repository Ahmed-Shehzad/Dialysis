using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIS.Contracts.IntegrationEvents;
using Dialysis.HIS.Integration.External;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIS.Integration;

public sealed class EhrPatientAdmittedStubConsumer(ILogger<EhrPatientAdmittedStubConsumer> logger) : IConsumer<PatientAdmittedToHospitalIntegrationEvent>
{
    public Task Handle(ConsumeContext<PatientAdmittedToHospitalIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation(
            "EHR ACL stub: patient {PatientId} admitted (MRN {Mrn}) at {AdmittedUtc}",
            m.PatientId,
            m.MedicalRecordNumber,
            m.AdmittedAtUtc);
        return Task.CompletedTask;
    }
}

public sealed class PdmsPatientAdmittedStubConsumer(ILogger<PdmsPatientAdmittedStubConsumer> logger) : IConsumer<PatientAdmittedToHospitalIntegrationEvent>
{
    public Task Handle(ConsumeContext<PatientAdmittedToHospitalIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation(
            "PDMS ACL stub: patient {PatientId} admitted (MRN {Mrn}) at {AdmittedUtc}",
            m.PatientId,
            m.MedicalRecordNumber,
            m.AdmittedAtUtc);
        return Task.CompletedTask;
    }
}

public sealed class EhrPatientDischargedStubConsumer(ILogger<EhrPatientDischargedStubConsumer> logger)
    : IConsumer<PatientDischargedIntegrationEvent>
{
    public Task Handle(ConsumeContext<PatientDischargedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation("EHR ACL stub: patient {PatientId} discharged at {DischargedUtc}", m.PatientId, m.DischargedAtUtc);
        return Task.CompletedTask;
    }
}

public sealed class PdmsReferralCreatedStubConsumer(ILogger<PdmsReferralCreatedStubConsumer> logger) : IConsumer<ReferralCreatedIntegrationEvent>
{
    public Task Handle(ConsumeContext<ReferralCreatedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation(
            "PDMS ACL stub: referral {ReferralId} for patient {PatientId} type {Type}",
            m.ReferralId,
            m.PatientId,
            m.ReferralTypeCode);
        return Task.CompletedTask;
    }
}

public sealed class LaboratoryReferralFromHisStubConsumer(
    ILaboratoryGateway laboratory,
    ILogger<LaboratoryReferralFromHisStubConsumer> logger) : IConsumer<ReferralCreatedIntegrationEvent>
{
    public async Task Handle(ConsumeContext<ReferralCreatedIntegrationEvent> context)
    {
        var m = context.Message;
        await laboratory.NotifyReferralCreatedStubAsync(m.ReferralId, m.ReferralTypeCode, context.CancellationToken)
            .ConfigureAwait(false);
        logger.LogInformation(
            "Laboratory ACL stub: referral {ReferralId} type {Type} handed to lab gateway",
            m.ReferralId,
            m.ReferralTypeCode);
    }
}

public sealed class SchedulingIntegrationLogConsumer(ILogger<SchedulingIntegrationLogConsumer> logger)
    : IConsumer<AppointmentBookedIntegrationEvent>
{
    public Task Handle(ConsumeContext<AppointmentBookedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation(
            "Operations/scheduling stub: appointment {AppointmentId} booked for patient {PatientId}",
            m.AppointmentId,
            m.PatientId);
        return Task.CompletedTask;
    }
}

public sealed class PharmacyMedicationOrderPlacedStubConsumer(
    IPharmacyGateway pharmacy,
    ILogger<PharmacyMedicationOrderPlacedStubConsumer> logger) : IConsumer<MedicationOrderPlacedIntegrationEvent>
{
    public async Task Handle(ConsumeContext<MedicationOrderPlacedIntegrationEvent> context)
    {
        var m = context.Message;
        var ack = await pharmacy.SubmitOrderStubAsync(m.MedicationCode, context.CancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Pharmacy ACL stub: order {OrderId} placed ({MedicationCode}), gateway ack {Ack}",
            m.OrderId,
            m.MedicationCode,
            ack);
    }
}

public sealed class PharmacyMedicationOrderDiscontinuedStubConsumer(ILogger<PharmacyMedicationOrderDiscontinuedStubConsumer> logger)
    : IConsumer<MedicationOrderDiscontinuedIntegrationEvent>
{
    public Task Handle(ConsumeContext<MedicationOrderDiscontinuedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation(
            "Pharmacy ACL stub: order {OrderId} discontinued ({MedicationCode}) at {Utc}",
            m.OrderId,
            m.MedicationCode,
            m.DiscontinuedAtUtc);
        return Task.CompletedTask;
    }
}
