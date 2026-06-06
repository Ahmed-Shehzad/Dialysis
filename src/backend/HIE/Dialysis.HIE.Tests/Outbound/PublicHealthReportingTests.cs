using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Outbound.Ports;
using Dialysis.HIE.Outbound.PublicHealth;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Outbound;

public sealed class PublicHealthReportingTests
{
    [Fact]
    public void Classifier_Matches_Configured_Codes_Case_Insensitively()
    {
        var classifier = new ConfiguredReportabilityClassifier(
            Options.Create(new PublicHealthReportingOptions { ReportableCodes = { "94500-6" } }));

        classifier.IsReportable("94500-6").ShouldBeTrue();
        classifier.IsReportable("94500-6".ToUpperInvariant()).ShouldBeTrue();
        classifier.IsReportable("12345-6").ShouldBeFalse();
        classifier.IsReportable(null).ShouldBeFalse();
    }

    [Fact]
    public async Task Reports_A_Reportable_Finding_To_The_Authority_Without_Consent_Async()
    {
        var store = new CapturingStore();
        var reporter = Reporter(store, "ph-authority", "94500-6");

        var reported = await reporter.ReportAsync(
            Guid.NewGuid(), new Observation { Id = "o1" }, "94500-6", CancellationToken.None);

        reported.ShouldBeTrue();
        var bundle = store.Added.ShouldHaveSingleItem();
        bundle.PartnerId.ShouldBe("ph-authority");
        bundle.Purpose.ShouldBe("PublicHealth");
        // The reporter has no consent-gate dependency: the bypass is structural, not a runtime branch.
    }

    [Fact]
    public async Task Does_Not_Report_A_Non_Reportable_Finding_Async()
    {
        var store = new CapturingStore();
        var reporter = Reporter(store, "ph-authority", "94500-6");

        (await reporter.ReportAsync(Guid.NewGuid(), new Observation { Id = "o1" }, "12345-6", CancellationToken.None))
            .ShouldBeFalse();
        store.Added.ShouldBeEmpty();
    }

    [Fact]
    public async Task Does_Nothing_When_Reporting_Is_Not_Configured_Async()
    {
        var store = new CapturingStore();
        var options = Options.Create(new PublicHealthReportingOptions()); // no authority, no codes
        var reporter = new PublicHealthReporter(
            store, new ConfiguredReportabilityClassifier(options), options, TimeProvider.System,
            NullLogger<PublicHealthReporter>.Instance);

        (await reporter.ReportAsync(Guid.NewGuid(), new Observation { Id = "o1" }, "94500-6", CancellationToken.None))
            .ShouldBeFalse();
        store.Added.ShouldBeEmpty();
    }

    private static PublicHealthReporter Reporter(IOutboundBundleStore store, string authority, string code)
    {
        var options = Options.Create(new PublicHealthReportingOptions
        {
            AuthorityPartnerId = authority,
            ReportableCodes = { code },
        });
        return new PublicHealthReporter(
            store, new ConfiguredReportabilityClassifier(options), options, TimeProvider.System,
            NullLogger<PublicHealthReporter>.Instance);
    }

    private sealed class CapturingStore : IOutboundBundleStore
    {
        public List<OutboundBundle> Added { get; } = [];
        public Task AddAsync(OutboundBundle bundle, CancellationToken cancellationToken = default)
        {
            Added.Add(bundle);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<OutboundBundle>> ClaimPendingAsync(int batchSize, DateTime asOfUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboundBundle>>([]);
        public Task<IReadOnlyList<OutboundBundle>> ListAsync(OutboundBundleStatus? statusFilter, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboundBundle>>([]);
        public Task<OutboundBundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OutboundBundle?>(null);
        public Task<IReadOnlyList<OutboundBundle>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboundBundle>>([]);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
