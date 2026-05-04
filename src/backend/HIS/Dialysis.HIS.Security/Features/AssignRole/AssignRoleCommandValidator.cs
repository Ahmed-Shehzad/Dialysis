using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.Security.Features.AssignRole;

public sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(static c => c.UserName, nameof(AssignRoleCommand.UserName)).NotEmpty();
        RuleFor(static c => c.RoleCode, nameof(AssignRoleCommand.RoleCode)).NotEmpty();
    }
}
