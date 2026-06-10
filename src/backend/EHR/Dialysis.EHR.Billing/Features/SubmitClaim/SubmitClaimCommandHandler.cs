using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.ChargeEdits;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Microsoft.Extensions.Logging;

namespace Dialysis.EHR.Billing.Features.SubmitClaim;

public sealed class SubmitClaimCommandHandler : ICommandHandler<SubmitClaimCommand, Guid>
{
    private readonly IChargeRepository _charges;
    private readonly IClaimRepository _claims;
    private readonly IPayerRepository _payers;
    private readonly IChargeEditChecker _editChecker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SubmitClaimCommandHandler> _logger;
    public SubmitClaimCommandHandler(IChargeRepository charges,
        IClaimRepository claims,
        IPayerRepository payers,
        IChargeEditChecker editChecker,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<SubmitClaimCommandHandler> logger)
    {
        _charges = charges;
        _claims = claims;
        _payers = payers;
        _editChecker = editChecker;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }
    public async Task<Guid> HandleAsync(SubmitClaimCommand request, CancellationToken cancellationToken)
    {
        var payer = await _payers.GetAsync(request.PayerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Payer '{request.PayerId}' not found.");
        if (!payer.IsActive)
            throw new InvalidOperationException($"Payer '{payer.PayerCode}' is inactive.");

        var resolved = new List<Charge>(request.ChargeIds.Count);
        foreach (var chargeId in request.ChargeIds)
        {
            var charge = await _charges.GetAsync(chargeId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Charge '{chargeId}' not found.");
            if (charge.PatientId != request.PatientId)
                throw new InvalidOperationException("Charges must all belong to the same patient.");
            resolved.Add(charge);
        }

        // Charge-review edits before the claim goes out — frequency / coverage / ABN, gated by the payer.
        var advisories = new List<ChargeAdvisory>();
        foreach (var charge in resolved)
        {
            var result = await _editChecker.CheckChargeAsync(
                charge.PatientId, charge.CptCode, [.. charge.DiagnosisPointerIcd10Codes], payer.PayerCode, cancellationToken)
                .ConfigureAwait(false);
            advisories.AddRange(result.Advisories);
        }
        var hasBlocking = advisories.Exists(a => a.Severity == ChargeAdvisorySeverity.Blocking);
        if (hasBlocking && !(request.AcknowledgeAdvisories && !string.IsNullOrWhiteSpace(request.OverrideReason)))
            throw new ChargeEditBlockedException(advisories);
        if (hasBlocking && request.AcknowledgeAdvisories)
        {
            _logger.LogInformation(
                "Claim submitted over {Count} blocking charge edit(s) by {OverriddenBy}: {Reason} (patient {PatientId}, payer {PayerCode}).",
                advisories.Count(a => a.Severity == ChargeAdvisorySeverity.Blocking),
                request.OverriddenBy ?? "biller", request.OverrideReason, request.PatientId, payer.PayerCode);
        }

        var claimId = Guid.CreateVersion7();
        var claim = Claim.Assemble(
            claimId,
            request.PatientId,
            payer.Id,
            payer.PayerCode,
            request.ClaimFormatCode,
            resolved);
        claim.Submit(GenerateControlNumber(_timeProvider.GetUtcNow().UtcDateTime), _timeProvider.GetUtcNow().UtcDateTime);

        _claims.Add(claim);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return claimId;
    }

    private static string GenerateControlNumber(DateTime utcNow) =>
        $"{utcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
