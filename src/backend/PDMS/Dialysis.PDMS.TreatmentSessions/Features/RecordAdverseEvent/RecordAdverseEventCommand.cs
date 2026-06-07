using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.RecordAdverseEvent;

/// <summary>
/// Records an intradialytic adverse event observed during a session (e.g. symptomatic hypotension,
/// muscle cramps, nausea). Publishes <c>IntradialyticAdverseEventIntegrationEvent</c> for the EHR
/// safety-surveillance read model to aggregate cross-patient.
/// </summary>
public sealed record RecordAdverseEventCommand : ICommand, IPermissionedCommand
{
    public RecordAdverseEventCommand(Guid SessionId, string EventKindCode, string Severity, string? Notes)
    {
        this.SessionId = SessionId;
        this.EventKindCode = EventKindCode;
        this.Severity = Severity;
        this.Notes = Notes;
    }
    public string RequiredPermission => PdmsPermissions.ReadingRecord;
    public Guid SessionId { get; init; }
    public string EventKindCode { get; init; }
    public string Severity { get; init; }
    public string? Notes { get; init; }
    public void Deconstruct(out Guid SessionId, out string EventKindCode, out string Severity, out string? Notes)
    {
        SessionId = this.SessionId;
        EventKindCode = this.EventKindCode;
        Severity = this.Severity;
        Notes = this.Notes;
    }
}
