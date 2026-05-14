using System.Text.Json;
using Dialysis.HIE.Core.Abstraction.OpenEhr;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.OpenEhr.Archetypes;

/// <summary>
/// Projects a haemodialysis FHIR <see cref="Procedure"/> to the openEHR
/// COMPOSITION.haemodialysis_session.v1 shape (procedure code, period, outcome, notes).
/// </summary>
public sealed class ProcedureArchetypeProjection : IArchetypeProjection<Procedure>
{
    public string ArchetypeId => ArchetypeIds.HaemodialysisSession;

    public string Project(Procedure resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        var period = resource.Performed as Period;
        var code = resource.Code?.Coding.FirstOrDefault();
        var payload = new
        {
            archetype_node_id = ArchetypeIds.HaemodialysisSession,
            content = new
            {
                procedure = new
                {
                    code = code?.Code,
                    system = code?.System,
                    display = code?.Display,
                },
                status = resource.Status?.ToString(),
                period = new
                {
                    start = period?.Start,
                    end = period?.End,
                },
                outcome = resource.Outcome?.Coding.FirstOrDefault()?.Code,
                status_reason = resource.StatusReason?.Coding.FirstOrDefault()?.Code,
                notes = resource.Note.Select(n => n.Text).Where(t => !string.IsNullOrWhiteSpace(t)),
            },
        };
        return JsonSerializer.Serialize(payload);
    }
}
