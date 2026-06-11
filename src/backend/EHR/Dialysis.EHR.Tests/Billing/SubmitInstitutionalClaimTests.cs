using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.ChargeEdits;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Features.SubmitClaim;
using Dialysis.EHR.Billing.Ports;
using Dialysis.EHR.Contracts.CodeSets;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Command-flow tests for the institutional (837I / UB-04) path through
/// <see cref="SubmitClaimCommandHandler"/>: an institutional format code plus the UB-04
/// section assembles a claim carrying <see cref="Claim.Institutional"/>, while the
/// professional default stays untouched (no section, format X12-837P).
/// </summary>
public sealed class SubmitInstitutionalClaimTests
{
    [Fact]
    public async Task Submit_Institutional_Claim_Persists_The_Ub04_Section_Async()
    {
        var patient = Guid.NewGuid();
        var charge = Charge.Capture(Guid.NewGuid(), patient, Guid.NewGuid(), "90999",
            ["N18.6"], new Money(250m, "USD"), revenueCode: "0821");
        var payer = Payer.Register(Guid.NewGuid(), "MEDICARE", "Medicare");
        var claims = new CapturingClaims();
        var handler = Handler(payer, charge, claims);

        var claimId = await handler.HandleAsync(
            new SubmitClaimCommand(patient, payer.Id, [charge.Id],
                ClaimFormatCode: EhrClaimFormats.Edi837Institutional,
                Institutional: new InstitutionalClaimRequest(
                    TypeOfBill: "0721",
                    StatementFromUtc: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    StatementToUtc: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    PrincipalProcedureCode: "5A1D70Z")),
            CancellationToken.None);

        var claim = claims.Added.ShouldHaveSingleItem();
        claim.Id.ShouldBe(claimId);
        claim.ClaimFormatCode.ShouldBe(EhrClaimFormats.Edi837Institutional);
        claim.Institutional.ShouldNotBeNull();
        claim.Institutional.TypeOfBill.ShouldBe("0721");
        claim.Institutional.PrincipalProcedureCode.ShouldBe("5A1D70Z");
    }

    [Fact]
    public async Task Submit_Institutional_Format_Without_The_Section_Fails_Async()
    {
        var patient = Guid.NewGuid();
        var charge = Charge.Capture(Guid.NewGuid(), patient, Guid.NewGuid(), "90999",
            ["N18.6"], new Money(250m, "USD"), revenueCode: "0821");
        var payer = Payer.Register(Guid.NewGuid(), "MEDICARE", "Medicare");
        var handler = Handler(payer, charge, new CapturingClaims());

        await Should.ThrowAsync<InvalidOperationException>(() => handler.HandleAsync(
            new SubmitClaimCommand(patient, payer.Id, [charge.Id],
                ClaimFormatCode: EhrClaimFormats.Edi837Institutional),
            CancellationToken.None));
    }

    [Fact]
    public async Task Submit_Defaults_To_The_Professional_Format_Async()
    {
        var patient = Guid.NewGuid();
        var charge = Charge.Capture(Guid.NewGuid(), patient, Guid.NewGuid(), "90935",
            ["N18.6"], new Money(250m, "USD"));
        var payer = Payer.Register(Guid.NewGuid(), "MEDICARE", "Medicare");
        var claims = new CapturingClaims();
        var handler = Handler(payer, charge, claims);

        await handler.HandleAsync(
            new SubmitClaimCommand(patient, payer.Id, [charge.Id]), CancellationToken.None);

        var claim = claims.Added.ShouldHaveSingleItem();
        claim.ClaimFormatCode.ShouldBe(EhrClaimFormats.Edi837Professional);
        claim.Institutional.ShouldBeNull();
    }

    private static SubmitClaimCommandHandler Handler(Payer payer, Charge charge, CapturingClaims claims) =>
        new(new SingleChargeRepo(charge), claims, new SinglePayerRepo(payer),
            new NoAdvisoryChecker(), new NoopUnitOfWork(), TimeProvider.System,
            NullLogger<SubmitClaimCommandHandler>.Instance);

    private sealed class NoAdvisoryChecker : IChargeEditChecker
    {
        public Task<ChargeAdvisoryResult> CheckChargeAsync(Guid patientId, string cptCode, IReadOnlyList<string> diagnosisPointerIcd10Codes, string? payerCode, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChargeAdvisoryResult([]));
    }

    private sealed class SingleChargeRepo : IChargeRepository
    {
        private readonly Charge _charge;
        public SingleChargeRepo(Charge charge) => _charge = charge;
        public Task<Charge?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Charge?>(_charge.Id == id ? _charge : null);
        public void Add(Charge charge) { }
        public Task<IReadOnlyList<Charge>> ListUnbilledForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
        public Task<IReadOnlyList<Charge>> ListAsync(ChargeStatus? status, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
        public Task<IReadOnlyList<Charge>> ListRecentForPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
        public Task<IReadOnlyList<Charge>> ListAgedCapturedAsync(DateTime capturedBeforeUtc, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
    }

    private sealed class CapturingClaims : IClaimRepository
    {
        public List<Claim> Added { get; } = [];
        public void Add(Claim claim) => Added.Add(claim);
        public Task<Claim?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Claim?>(Added.FirstOrDefault(c => c.Id == id));
        public Task<Claim?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default) => Task.FromResult<Claim?>(null);
        public Task<IReadOnlyList<Claim>> ListAsync(ClaimStatus? status, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Claim>>([]);
    }

    private sealed class SinglePayerRepo : IPayerRepository
    {
        private readonly Payer _payer;
        public SinglePayerRepo(Payer payer) => _payer = payer;
        public Task<Payer?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Payer?>(_payer.Id == id ? _payer : null);
        public Task<Payer?> FindByCodeAsync(string payerCode, CancellationToken cancellationToken = default) => Task.FromResult<Payer?>(_payer);
        public void Add(Payer payer) { }
    }

    private sealed class NoopUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
