using System.Text;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Serialization;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Contracts.Integration;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.DeactivateUser;

public sealed class DeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand>
{
    private readonly IUserAccountRepository _users;
    private readonly ITransponderOutbox _outbox;
    private readonly IMessageSerializer _serializer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public DeactivateUserCommandHandler(IUserAccountRepository users,
        ITransponderOutbox outbox,
        IMessageSerializer serializer,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _outbox = outbox;
        _serializer = serializer;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetAsync(request.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"User '{request.UserId}' not found.");

        if (user.Status == UserAccountStatus.Deactivated)
            return Unit.Value;

        user.Status = UserAccountStatus.Deactivated;
        _users.Update(user);

        // Rides the transactional outbox: the account state change and the cross-context
        // signal commit together in the SaveChanges below — never published manually.
        var @event = new UserDeactivatedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredOn: _timeProvider.GetUtcNow().UtcDateTime,
                SchemaVersion: 1,
                UserId: user.Id,
                Subject: user.Subject);
        var payload = _serializer.Serialize(@event);
        await _outbox.EnqueueAsync(new TransponderOutboxEnvelope(
            AssemblyQualifiedEventType: typeof(UserDeactivatedIntegrationEvent).AssemblyQualifiedName!,
            PayloadJson: Encoding.UTF8.GetString(payload.Span),
            Id: @event.EventId),
            cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
