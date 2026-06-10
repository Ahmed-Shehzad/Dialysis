using System.Text;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Serialization;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Contracts.Integration;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.ProvisionUser;

public sealed class ProvisionUserCommandHandler : ICommandHandler<ProvisionUserCommand, Guid>
{
    private readonly IUserAccountRepository _users;
    private readonly ITransponderOutbox _outbox;
    private readonly IMessageSerializer _serializer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public ProvisionUserCommandHandler(IUserAccountRepository users,
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
    public async Task<Guid> HandleAsync(ProvisionUserCommand request, CancellationToken cancellationToken)
    {
        if (await _users.FindBySubjectAsync(request.Subject, cancellationToken).ConfigureAwait(false) is { } existing)
            return existing.Id;

        var id = Guid.CreateVersion7();
        var user = new UserAccount
        {
            Id = id,
            Subject = request.Subject,
            DisplayName = request.DisplayName,
            Email = request.Email,
            Status = UserAccountStatus.Provisioned,
        };
        _users.Add(user);

        var @event = new UserRegisteredIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: _timeProvider.GetUtcNow().UtcDateTime,
            SchemaVersion: 1,
            UserId: id,
            Subject: request.Subject,
            DisplayName: request.DisplayName,
            Email: request.Email);

        var payload = _serializer.Serialize(@event);
        await _outbox.EnqueueAsync(new TransponderOutboxEnvelope(
            AssemblyQualifiedEventType: typeof(UserRegisteredIntegrationEvent).AssemblyQualifiedName!,
            PayloadJson: Encoding.UTF8.GetString(payload.Span),
            Id: @event.EventId),
            cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
