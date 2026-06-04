using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.DefineRole;

public sealed class DefineRoleCommandHandler : ICommandHandler<DefineRoleCommand, Guid>
{
    private readonly IRoleDefinitionRepository _roles;
    private readonly IUnitOfWork _unitOfWork;
    public DefineRoleCommandHandler(IRoleDefinitionRepository roles,
        IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(DefineRoleCommand request, CancellationToken cancellationToken)
    {
        if (await _roles.FindByCodeAsync(request.Code, cancellationToken).ConfigureAwait(false) is { } existing)
            return existing.Id;

        var id = Guid.CreateVersion7();
        var role = new RoleDefinition
        {
            Id = id,
            Code = request.Code,
            DisplayName = request.DisplayName,
            Permissions = [.. request.Permissions],
        };
        _roles.Add(role);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
