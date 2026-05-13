using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.DefineRole;

public sealed class DefineRoleCommandHandler(
    IRoleDefinitionRepository roles,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DefineRoleCommand, Guid>
{
    public async Task<Guid> Handle(DefineRoleCommand request, CancellationToken cancellationToken)
    {
        if (await roles.FindByCodeAsync(request.Code, cancellationToken).ConfigureAwait(false) is { } existing)
            return existing.Id;

        var id = Guid.CreateVersion7();
        var role = new RoleDefinition
        {
            Id = id,
            Code = request.Code,
            DisplayName = request.DisplayName,
            Permissions = [.. request.Permissions],
        };
        roles.Add(role);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
