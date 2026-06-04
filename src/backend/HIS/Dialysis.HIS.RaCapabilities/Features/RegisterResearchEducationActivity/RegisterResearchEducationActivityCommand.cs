using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterResearchEducationActivity;

public sealed record RegisterResearchEducationActivityCommand : ICommand<Guid>, IPermissionedCommand
{
    public RegisterResearchEducationActivityCommand(string ActivityKindCode,
        string Title,
        string ExternalReference,
        DateTime? RecordedAtUtc = null)
    {
        this.ActivityKindCode = ActivityKindCode;
        this.Title = Title;
        this.ExternalReference = ExternalReference;
        this.RecordedAtUtc = RecordedAtUtc;
    }
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
    public string ActivityKindCode { get; init; }
    public string Title { get; init; }
    public string ExternalReference { get; init; }
    public DateTime? RecordedAtUtc { get; init; }
    public void Deconstruct(out string ActivityKindCode, out string Title, out string ExternalReference, out DateTime? RecordedAtUtc)
    {
        ActivityKindCode = this.ActivityKindCode;
        Title = this.Title;
        ExternalReference = this.ExternalReference;
        RecordedAtUtc = this.RecordedAtUtc;
    }
}
