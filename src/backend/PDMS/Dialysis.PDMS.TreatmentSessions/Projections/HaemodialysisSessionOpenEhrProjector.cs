using System.Text.Json;
using Dialysis.PDMS.Contracts.CodeSets;
using Dialysis.PDMS.TreatmentSessions.Domain;

namespace Dialysis.PDMS.TreatmentSessions.Projections;

/// <summary>
/// Projects a <see cref="DialysisSession"/> into <c>openEHR-EHR-COMPOSITION.haemodialysis_session.v1</c>
/// at a specific lifecycle phase (started / completed / aborted).
/// </summary>
public sealed class HaemodialysisSessionOpenEhrProjector
{
    public OpenEhrProjection Project(DialysisSession session, HaemodialysisSessionPhase phase, DateTime phaseAtUtc)
    {
        ArgumentNullException.ThrowIfNull(session);

        var compositionJson = JsonSerializer.Serialize(new
        {
            archetype_node_id = PdmsOpenEhrArchetypes.HaemodialysisSession,
            phase = phase.ToString().ToLowerInvariant(),
            context = new
            {
                start_time = session.ActualStartUtc,
                end_time = session.ActualEndUtc,
                phase_time = phaseAtUtc,
                machine_id = session.MachineId,
            },
            data = new
            {
                prescription = new
                {
                    dialyzer_model = session.Prescription.DialyzerModel,
                    blood_flow_rate_ml_per_min = session.Prescription.BloodFlowRateMlPerMin,
                    dialysate_flow_rate_ml_per_min = session.Prescription.DialysateFlowRateMlPerMin,
                    target_uf_volume_l = session.Prescription.TargetUfVolumeLiters,
                    prescribed_duration_min = session.Prescription.PrescribedDurationMinutes,
                    anticoagulation_protocol = session.Prescription.AnticoagulationProtocolCode,
                },
                vascular_access = new
                {
                    type = session.Access.Kind.ToString(),
                    site = session.Access.Site,
                },
                outcome = new
                {
                    status = session.Status.ToString(),
                    achieved_uf_volume_l = session.AchievedUfVolumeLiters,
                    abort_reason_code = session.AbortReasonCode,
                },
            },
        });

        return new OpenEhrProjection(PdmsOpenEhrArchetypes.HaemodialysisSession, compositionJson);
    }
}

public sealed record OpenEhrProjection
{
    public OpenEhrProjection(string ArchetypeId, string CompositionJson)
    {
        this.ArchetypeId = ArchetypeId;
        this.CompositionJson = CompositionJson;
    }
    public string ArchetypeId { get; init; }
    public string CompositionJson { get; init; }
    public void Deconstruct(out string ArchetypeId, out string CompositionJson)
    {
        ArchetypeId = this.ArchetypeId;
        CompositionJson = this.CompositionJson;
    }
}
