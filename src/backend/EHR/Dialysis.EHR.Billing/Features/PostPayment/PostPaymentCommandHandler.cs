using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;

namespace Dialysis.EHR.Billing.Features.PostPayment;

public sealed class PostPaymentCommandHandler : ICommandHandler<PostPaymentCommand, Guid>
{
    private readonly IPaymentRepository _payments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public PostPaymentCommandHandler(IPaymentRepository payments,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _payments = payments;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
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
            _timeProvider.GetUtcNow().UtcDateTime,
            request.ExternalReference);
        _payments.Add(payment);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
