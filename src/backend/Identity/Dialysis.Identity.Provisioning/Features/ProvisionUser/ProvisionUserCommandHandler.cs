using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Contracts.Integration;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.ProvisionUser;

public sealed class ProvisionUserCommandHandler(
    IUserAccountRepository users,
    ITransponderOutbox outbox,
    IMessageSerializer serializer,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ProvisionUserCommand, Guid>
{
    public async Task<Guid> Handle(ProvisionUserCommand request, CancellationToken cancellationToken)
    {
        if (await users.FindBySubjectAsync(request.Subject, cancellationToken).ConfigureAwait(false) is { } existing)
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
        users.Add(user);

        var @event = new UserRegisteredIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: timeProvider.GetUtcNow().UtcDateTime,
            SchemaVersion: 1,
            UserId: id,
            Subject: request.Subject,
            DisplayName: request.DisplayName,
            Email: request.Email);

        var payload = serializer.Serialize(@event);
        await outbox.EnqueueAsync(new TransponderOutboxEnvelope(
            AssemblyQualifiedEventType: typeof(UserRegisteredIntegrationEvent).AssemblyQualifiedName!,
            PayloadJson: System.Text.Encoding.UTF8.GetString(payload.Span),
            Id: @event.EventId),
            cancellationToken).ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
