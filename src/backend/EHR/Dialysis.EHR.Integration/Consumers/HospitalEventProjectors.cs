using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Integration.Ports;
using Dialysis.EHR.Integration.ReadModels;
using Dialysis.HIE.Contracts.Integration;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;

namespace Dialysis.EHR.Integration.Consumers;

/// <summary>Records a HIS admission into the care-coordination hospital-event read model.</summary>
public sealed class PatientAdmittedHospitalEventProjector : IConsumer<PatientAdmittedIntegrationEvent>
{
    private readonly IHospitalEventRepository _events;
    private readonly IUnitOfWork _unitOfWork;
    public PatientAdmittedHospitalEventProjector(IHospitalEventRepository events, IUnitOfWork unitOfWork)
    {
        _events = events;
        _unitOfWork = unitOfWork;
    }
    public async Task HandleAsync(ConsumeContext<PatientAdmittedIntegrationEvent> context)
    {
        var m = context.Message;
        await _events.RecordAsync(new HospitalEvent
        {
            Id = Guid.CreateVersion7(),
            PatientId = m.PatientId,
            Kind = HospitalEventKind.Admitted,
            Source = "HIS",
            OccurredAtUtc = m.AdmittedAtUtc,
            Detail = $"Admitted to ward {m.WardCode}",
            SourceEventKey = m.AdmissionId.ToString(),
        }, context.CancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Records a HIS discharge into the hospital-event read model — the proactive-follow-up trigger.</summary>
public sealed class PatientDischargedHospitalEventProjector : IConsumer<PatientDischargedIntegrationEvent>
{
    private readonly IHospitalEventRepository _events;
    private readonly IUnitOfWork _unitOfWork;
    public PatientDischargedHospitalEventProjector(IHospitalEventRepository events, IUnitOfWork unitOfWork)
    {
        _events = events;
        _unitOfWork = unitOfWork;
    }
    public async Task HandleAsync(ConsumeContext<PatientDischargedIntegrationEvent> context)
    {
        var m = context.Message;
        await _events.RecordAsync(new HospitalEvent
        {
            Id = Guid.CreateVersion7(),
            PatientId = m.PatientId,
            Kind = HospitalEventKind.Discharged,
            Source = "HIS",
            OccurredAtUtc = m.DischargedAtUtc,
            Detail = $"Discharged from ward {m.WardCode}",
            SourceEventKey = m.AdmissionId.ToString(),
        }, context.CancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Records an outside-org encounter (HIE inbound) into the hospital-event read model. The partner patient
/// id is opaque (EHR has no MPI cross-reference), so the row is unmatched (PatientId null) and surfaces on
/// the worklist for a coordinator to link manually. A future ExternalPatientReference consumer would
/// resolve PatientId automatically.
/// </summary>
public sealed class ExternalEncounterHospitalEventProjector : IConsumer<ExternalEncounterIngestedIntegrationEvent>
{
    private readonly IHospitalEventRepository _events;
    private readonly IUnitOfWork _unitOfWork;
    public ExternalEncounterHospitalEventProjector(IHospitalEventRepository events, IUnitOfWork unitOfWork)
    {
        _events = events;
        _unitOfWork = unitOfWork;
    }
    public async Task HandleAsync(ConsumeContext<ExternalEncounterIngestedIntegrationEvent> context)
    {
        var m = context.Message;
        var detail = string.Join(" · ",
            new[] { m.ClassCode, m.ReasonCode }.Where(s => !string.IsNullOrWhiteSpace(s)));
        await _events.RecordAsync(new HospitalEvent
        {
            Id = Guid.CreateVersion7(),
            PatientId = null,
            Kind = HospitalEventKind.ExternalEncounter,
            Source = m.PartnerId,
            OccurredAtUtc = m.PeriodStartUtc ?? m.OccurredOn,
            Detail = detail.Length > 0 ? detail : "External encounter",
            ExternalPatientRef = m.PatientExternalLogicalId,
            SourceEventKey = $"{m.PartnerId}:{m.ExternalLogicalId}",
        }, context.CancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}
