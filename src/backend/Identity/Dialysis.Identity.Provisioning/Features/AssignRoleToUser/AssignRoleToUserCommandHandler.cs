using System.Text;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Serialization;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Contracts.Integration;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.AssignRoleToUser;

public sealed class AssignRoleToUserCommandHandler : ICommandHandler<AssignRoleToUserCommand>
{
    private readonly IUserAccountRepository _users;
    private readonly IRoleDefinitionRepository _roles;
    private readonly IRoleAssignmentRepository _assignments;
    private readonly ITransponderOutbox _outbox;
    private readonly IMessageSerializer _serializer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public AssignRoleToUserCommandHandler(IUserAccountRepository users,
        IRoleDefinitionRepository roles,
        IRoleAssignmentRepository assignments,
        ITransponderOutbox outbox,
        IMessageSerializer serializer,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _roles = roles;
        _assignments = assignments;
        _outbox = outbox;
        _serializer = serializer;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetAsync(request.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"User '{request.UserId}' not found.");
        if (user.Status == UserAccountStatus.Deactivated)
            throw new InvalidOperationException($"User '{request.UserId}' is deactivated.");

        var role = await _roles.FindByCodeAsync(request.RoleCode, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Role '{request.RoleCode}' not defined.");

        if (await _assignments.FindAsync(user.Id, role.Id, cancellationToken).ConfigureAwait(false) is not null)
            return Unit.Value;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        _assignments.Add(new RoleAssignment
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            RoleId = role.Id,
            AssignedAtUtc = now,
        });

        var @event = new RoleAssignedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: now,
            SchemaVersion: 1,
            UserId: user.Id,
            Subject: user.Subject,
            RoleCode: role.Code,
            Permissions: [.. role.Permissions]);

        var payload = _serializer.Serialize(@event);
        await _outbox.EnqueueAsync(new TransponderOutboxEnvelope(
            AssemblyQualifiedEventType: typeof(RoleAssignedIntegrationEvent).AssemblyQualifiedName!,
            PayloadJson: Encoding.UTF8.GetString(payload.Span),
            Id: @event.EventId),
            cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
