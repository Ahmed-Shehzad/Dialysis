using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;

namespace Dialysis.EHR.Billing.Features.SubmitClaim;

public sealed class SubmitClaimCommandHandler(
    IChargeRepository charges,
    IClaimRepository claims,
    IPayerRepository payers,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<SubmitClaimCommand, Guid>
{
    public async Task<Guid> Handle(SubmitClaimCommand request, CancellationToken cancellationToken)
    {
        var payer = await payers.GetAsync(request.PayerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Payer '{request.PayerId}' not found.");
        if (!payer.IsActive)
            throw new InvalidOperationException($"Payer '{payer.PayerCode}' is inactive.");

        var resolved = new List<Charge>(request.ChargeIds.Count);
        foreach (var chargeId in request.ChargeIds)
        {
            var charge = await charges.GetAsync(chargeId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Charge '{chargeId}' not found.");
            if (charge.PatientId != request.PatientId)
                throw new InvalidOperationException("Charges must all belong to the same patient.");
            resolved.Add(charge);
        }

        var claimId = Guid.CreateVersion7();
        var claim = Claim.Assemble(
            claimId,
            request.PatientId,
            payer.Id,
            payer.PayerCode,
            request.ClaimFormatCode,
            resolved);
        claim.Submit(GenerateControlNumber(timeProvider.GetUtcNow().UtcDateTime), timeProvider.GetUtcNow().UtcDateTime);

        claims.Add(claim);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return claimId;
    }

    private static string GenerateControlNumber(DateTime utcNow) =>
        $"{utcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
