using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;

namespace Dialysis.EHR.Billing.Features.PostPayment;

public sealed class PostPaymentCommandHandler(
    IPaymentRepository payments,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<PostPaymentCommand, Guid>
{
    public async Task<Guid> HandleAsync(PostPaymentCommand request, CancellationToken cancellationToken)
    {
        var amount = new Money(request.Amount, request.CurrencyCode);
        var id = Guid.CreateVersion7();
        var payment = Payment.Post(
            id,
            request.PatientId,
            request.ClaimId,
            amount,
            request.Method,
            timeProvider.GetUtcNow().UtcDateTime,
            request.ExternalReference);
        payments.Add(payment);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
