using Dialysis.EHR.Billing.ChargeEdits;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

public sealed class ChargeEditCheckerTests
{
    private static readonly DateTime _nowUtc = new(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc);

    private static ChargeEditChecker Checker(ChargeEditOptions options, params Charge[] existing) =>
        new(new StubCharges(existing), new FixedClock(_nowUtc), Options.Create(options));

    private static Charge ChargeFor(Guid patientId, string cpt) =>
        Charge.Capture(Guid.NewGuid(), patientId, Guid.NewGuid(), cpt, ["N18.6"], new Money(100m, "USD"));

    [Fact]
    public async Task Empty_Options_Are_A_Noop_Async()
    {
        var result = await Checker(new ChargeEditOptions())
            .CheckChargeAsync(Guid.NewGuid(), "90935", ["N18.6"], payerCode: null, CancellationToken.None);
        result.Advisories.ShouldBeEmpty();
    }

    [Fact]
    public async Task Frequency_Limit_Exceeded_Raises_A_Warning_By_Default_Async()
    {
        var patient = Guid.NewGuid();
        var options = new ChargeEditOptions { FrequencyLimits = { new CptFrequencyRule { CptCode = "90935", MaxOccurrences = 1 } } };

        // One same-CPT charge already exists in the window → the prospective charge is the 2nd.
        var result = await Checker(options, ChargeFor(patient, "90935"))
            .CheckChargeAsync(patient, "90935", ["N18.6"], payerCode: null, CancellationToken.None);

        var advisory = result.Advisories.ShouldHaveSingleItem();
        advisory.Category.ShouldBe(ChargeAdvisoryCategory.CptFrequencyLimitExceeded);
        advisory.Severity.ShouldBe(ChargeAdvisorySeverity.Warning);
        result.HasBlocking.ShouldBeFalse();
    }

    [Fact]
    public async Task Frequency_Limit_Can_Be_Configured_Blocking_Async()
    {
        var patient = Guid.NewGuid();
        var options = new ChargeEditOptions { FrequencyLimits = { new CptFrequencyRule { CptCode = "90935", MaxOccurrences = 1, Blocking = true } } };

        var result = await Checker(options, ChargeFor(patient, "90935"))
            .CheckChargeAsync(patient, "90935", ["N18.6"], payerCode: null, CancellationToken.None);

        result.Advisories.ShouldHaveSingleItem().Severity.ShouldBe(ChargeAdvisorySeverity.Blocking);
        result.HasBlocking.ShouldBeTrue();
    }

    [Fact]
    public async Task Coverage_Rule_Flags_A_Missing_Required_Diagnosis_Async()
    {
        var options = new ChargeEditOptions { CoverageRules = { new CptCoverageRule { CptCode = "80053", RequiredAnyIcd10 = { "E11.9" } } } };

        var result = await Checker(options)
            .CheckChargeAsync(Guid.NewGuid(), "80053", ["N18.6"], payerCode: null, CancellationToken.None);

        result.Advisories.ShouldHaveSingleItem().Category.ShouldBe(ChargeAdvisoryCategory.MissingRequiredDiagnosis);
    }

    [Fact]
    public async Task Coverage_Rule_Satisfied_Raises_Nothing_Async()
    {
        var options = new ChargeEditOptions { CoverageRules = { new CptCoverageRule { CptCode = "80053", RequiredAnyIcd10 = { "E11.9" } } } };

        var result = await Checker(options)
            .CheckChargeAsync(Guid.NewGuid(), "80053", ["E11.9"], payerCode: null, CancellationToken.None);

        result.Advisories.ShouldBeEmpty();
    }

    [Fact]
    public async Task Medicare_Payer_Escalates_A_Firing_Edit_To_Abn_Required_Async()
    {
        var options = new ChargeEditOptions
        {
            CoverageRules = { new CptCoverageRule { CptCode = "80053", RequiredAnyIcd10 = { "E11.9" } } },
            MedicarePayerCodes = { "MEDICARE" },
        };

        var result = await Checker(options)
            .CheckChargeAsync(Guid.NewGuid(), "80053", ["N18.6"], payerCode: "MEDICARE", CancellationToken.None);

        result.Advisories.ShouldContain(a => a.Category == ChargeAdvisoryCategory.AbnRequired && a.Severity == ChargeAdvisorySeverity.Blocking);
        result.HasBlocking.ShouldBeTrue();
    }

    private sealed class StubCharges : IChargeRepository
    {
        private readonly List<Charge> _charges;
        public StubCharges(IEnumerable<Charge> charges) => _charges = [.. charges];
        public Task<IReadOnlyList<Charge>> ListRecentForPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Charge>>([.. _charges.Where(c => c.PatientId == patientId)]);
        public Task<Charge?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Charge?>(null);
        public Task<IReadOnlyList<Charge>> ListUnbilledForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
        public Task<IReadOnlyList<Charge>> ListAsync(ChargeStatus? status, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
        public Task<IReadOnlyList<Charge>> ListAgedCapturedAsync(DateTime capturedBeforeUtc, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
        public void Add(Charge charge) { }
    }

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTime _utcNow;
        public FixedClock(DateTime utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }
}
