using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.ResumeSession;

/// <summary>Resumes a paused session, closing the open pause span so usage time keeps excluding it.</summary>
public sealed record ResumeSessionCommand : ICommand, IPermissionedCommand
{
    /// <summary>Resumes a paused session.</summary>
    public ResumeSessionCommand(Guid SessionId) => this.SessionId = SessionId;
    public string RequiredPermission => PdmsPermissions.SessionResume;
    public Guid SessionId { get; init; }
    public void Deconstruct(out Guid SessionId) => SessionId = this.SessionId;
}
