using Dialysis.CQRS.Queries;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Provisioning.Features.ListRoles;

public sealed record RoleSummaryDto
{
    public RoleSummaryDto(Guid Id, string Code, string DisplayName, IReadOnlyList<string> Permissions)
    {
        this.Id = Id;
        this.Code = Code;
        this.DisplayName = DisplayName;
        this.Permissions = Permissions;
    }
    public Guid Id { get; init; }
    public string Code { get; init; }
    public string DisplayName { get; init; }
    public IReadOnlyList<string> Permissions { get; init; }
    public void Deconstruct(out Guid Id, out string Code, out string DisplayName, out IReadOnlyList<string> Permissions)
    {
        Id = this.Id;
        Code = this.Code;
        DisplayName = this.DisplayName;
        Permissions = this.Permissions;
    }
}

public sealed record ListRolesQuery : IQuery<IReadOnlyList<RoleSummaryDto>>, IPermissionedCommand
{
    public string RequiredPermission => IdentityPermissions.RoleRead;
}
