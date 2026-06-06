using Dialysis.EHR.Billing.ChargeEdits;
using Dialysis.EHR.Billing.Coding;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests.Billing;

public sealed class UnderCodingAdvisoryTests
{
    private static readonly DateTime _nowUtc = new(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc);

    private static ChargeEditChecker Checker()
    {
        var em = new EmCodingOptions();
        em.Levels.Add(new EmLevelRule { CptCode = "99213", Level = 3, MinDiagnoses = 1, Rationale = "1+ problems" });
        em.Levels.Add(new EmLevelRule { CptCode = "99214", Level = 4, MinDiagnoses = 2, Rationale = "2+ problems" });
        em.Levels.Add(new EmLevelRule { CptCode = "99215", Level = 5, MinDiagnoses = 4, Rationale = "4+ problems" });
        var coder = new EvaluationManagementCoder(Options.Create(em));
        // Charge-edit frequency/coverage off → only the E/M under-coding check is exercised.
        return new ChargeEditChecker(new EmptyCharges(), new FixedClock(_nowUtc), Options.Create(new ChargeEditOptions()), coder);
    }

    [Fact]
    public async Task Captured_Level_Below_Documentation_Raises_A_Non_Blocking_Opportunity_Async()
    {
        // 2 diagnoses support 99214, but the visit was captured as 99213.
        var result = await Checker().CheckChargeAsync(Guid.NewGuid(), "99213", ["E11.9", "I10"], payerCode: null, CancellationToken.None);

        var advisory = result.Advisories.ShouldHaveSingleItem();
        advisory.Category.ShouldBe(ChargeAdvisoryCategory.UnderCodingOpportunity);
        advisory.Severity.ShouldBe(ChargeAdvisorySeverity.Warning);
        advisory.MatchedCode.ShouldBe("99214");
        result.HasBlocking.ShouldBeFalse();
    }

    [Fact]
    public async Task Captured_Level_That_Matches_Documentation_Is_Quiet_Async()
    {
        var result = await Checker().CheckChargeAsync(Guid.NewGuid(), "99214", ["E11.9", "I10"], payerCode: null, CancellationToken.None);
        result.Advisories.ShouldBeEmpty();
    }

    [Fact]
    public async Task Non_Em_Cpt_Is_Not_Evaluated_Async()
    {
        // A venipuncture code, not an E/M code → no under-coding advisory regardless of diagnoses.
        var result = await Checker().CheckChargeAsync(Guid.NewGuid(), "36415", ["E11.9", "I10", "N18.6", "E78.5"], payerCode: null, CancellationToken.None);
        result.Advisories.ShouldBeEmpty();
    }

    private sealed class EmptyCharges : IChargeRepository
    {
        public Task<Charge?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Charge?>(null);
        public Task<IReadOnlyList<Charge>> ListUnbilledForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
        public Task<IReadOnlyList<Charge>> ListAsync(ChargeStatus? status, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
        public Task<IReadOnlyList<Charge>> ListRecentForPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Charge>>([]);
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
