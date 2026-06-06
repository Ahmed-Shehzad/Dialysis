using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.ChargeEdits;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Features.CaptureCharge;
using Dialysis.EHR.Billing.Features.SubmitClaim;
using Dialysis.EHR.Billing.Ports;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// The charge-edit gate at the two chokepoints: a blocking edit holds the charge/claim unless an
/// acknowledged override with a reason is supplied.
/// </summary>
public sealed class ChargeEditGateTests
{
    private static ChargeAdvisoryResult Blocking() => new(
        [new ChargeAdvisory(ChargeAdvisoryCategory.CptFrequencyLimitExceeded, ChargeAdvisorySeverity.Blocking, "90935", "2 in 365d", "Over limit")]);

    [Fact]
    public async Task Capture_Blocks_Unacknowledged_Edit_And_Persists_Nothing_Async()
    {
        var repo = new CapturingCharges();
        var handler = new CaptureChargeCommandHandler(repo, new FakeChecker(Blocking()), new NoopUnitOfWork());

        await Should.ThrowAsync<ChargeEditBlockedException>(() => handler.HandleAsync(
            new CaptureChargeCommand(Guid.NewGuid(), Guid.NewGuid(), "90935", ["N18.6"], 100m, "USD"),
            CancellationToken.None));

        repo.Added.ShouldBeEmpty();
    }

    [Fact]
    public async Task Capture_Override_Persists_The_Reason_On_The_Charge_Async()
    {
        var repo = new CapturingCharges();
        var handler = new CaptureChargeCommandHandler(repo, new FakeChecker(Blocking()), new NoopUnitOfWork());

        await handler.HandleAsync(
            new CaptureChargeCommand(Guid.NewGuid(), Guid.NewGuid(), "90935", ["N18.6"], 100m, "USD",
                AcknowledgeAdvisories: true, OverrideReason: "Documented medical necessity", OverriddenBy: "biller-3"),
            CancellationToken.None);

        var charge = repo.Added.ShouldHaveSingleItem();
        charge.OverrideReason.ShouldBe("Documented medical necessity");
        charge.OverriddenBy.ShouldBe("biller-3");
    }

    [Fact]
    public async Task Capture_Acknowledge_Without_Reason_Still_Blocks_Async()
    {
        var repo = new CapturingCharges();
        var handler = new CaptureChargeCommandHandler(repo, new FakeChecker(Blocking()), new NoopUnitOfWork());

        await Should.ThrowAsync<ChargeEditBlockedException>(() => handler.HandleAsync(
            new CaptureChargeCommand(Guid.NewGuid(), Guid.NewGuid(), "90935", ["N18.6"], 100m, "USD",
                AcknowledgeAdvisories: true, OverrideReason: "  "),
            CancellationToken.None));
        repo.Added.ShouldBeEmpty();
    }

    [Fact]
    public async Task Submit_Blocks_Unacknowledged_Edit_Async()
    {
        var patient = Guid.NewGuid();
        var charge = Charge.Capture(Guid.NewGuid(), patient, Guid.NewGuid(), "90935", ["N18.6"], new Money(100m, "USD"));
        var payer = Payer.Register(Guid.NewGuid(), "MEDICARE", "Medicare");
        var handler = SubmitHandler(payer, charge, new FakeChecker(Blocking()));

        await Should.ThrowAsync<ChargeEditBlockedException>(() => handler.HandleAsync(
            new SubmitClaimCommand(patient, payer.Id, [charge.Id]), CancellationToken.None));
    }

    [Fact]
    public async Task Submit_Override_Proceeds_Async()
    {
        var patient = Guid.NewGuid();
        var charge = Charge.Capture(Guid.NewGuid(), patient, Guid.NewGuid(), "90935", ["N18.6"], new Money(100m, "USD"));
        var payer = Payer.Register(Guid.NewGuid(), "MEDICARE", "Medicare");
        var claims = new CapturingClaims();
        var handler = SubmitHandler(payer, charge, new FakeChecker(Blocking()), claims);

        var claimId = await handler.HandleAsync(
            new SubmitClaimCommand(patient, payer.Id, [charge.Id],
                AcknowledgeAdvisories: true, OverrideReason: "ABN obtained · #A-1009"),
            CancellationToken.None);

        claims.Added.ShouldHaveSingleItem().Id.ShouldBe(claimId);
    }

    private static SubmitClaimCommandHandler SubmitHandler(
        Payer payer, Charge charge, IChargeEditChecker checker, CapturingClaims? claims = null) =>
        new(new SingleChargeRepo(charge), claims ?? new CapturingClaims(), new SinglePayerRepo(payer),
            checker, new NoopUnitOfWork(), TimeProvider.System, NullLogger<SubmitClaimCommandHandler>.Instance);

    private sealed class FakeChecker : IChargeEditChecker
    {
        private readonly ChargeAdvisoryResult _result;
        public FakeChecker(ChargeAdvisoryResult result) => _result = result;
        public Task<ChargeAdvisoryResult> CheckChargeAsync(Guid patientId, string cptCode, IReadOnlyList<string> diagnosisPointerIcd10Codes, string? payerCode, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class CapturingCharges : IChargeRepository
    {
        public List<Charge> Added { get; } = [];
        public void Add(Charge charge) => Added.Add(charge);
        public Task<Charge?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Charge?>(Added.FirstOrDefault(c => c.Id == id));
        public Task<IReadOnlyList<Charge>> ListUnbilledForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
        public Task<IReadOnlyList<Charge>> ListAsync(ChargeStatus? status, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
        public Task<IReadOnlyList<Charge>> ListRecentForPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
        public Task<IReadOnlyList<Charge>> ListAgedCapturedAsync(DateTime capturedBeforeUtc, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
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
