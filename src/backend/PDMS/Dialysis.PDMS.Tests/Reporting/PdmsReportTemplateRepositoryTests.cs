using Dialysis.DomainDrivenDesign.Specifications;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Reporting.Domain;
using Dialysis.PDMS.Reporting.Generators;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// Proves the concrete repository delegates to <see cref="ReportTemplateResolver"/> over the
/// shared PDMS repository: language-aware lookup with default fallback, end-to-end.
/// </summary>
public sealed class PdmsReportTemplateRepositoryTests
{
    [Fact]
    public async Task Find_Active_Resolves_Preferred_Language_Then_Default_Async()
    {
        var store = new FakeRepo();
        await store.AddAsync(Published("discharge", "de"), CancellationToken.None);
        await store.AddAsync(Published("discharge-default", null), CancellationToken.None);
        var repo = new PdmsReportTemplateRepository(store);

        var de = await repo.FindActiveAsync(ReportKind.DischargeLetter, "de-DE", CancellationToken.None);
        de!.LanguageCode.ShouldBe("de");

        var fallback = await repo.FindActiveAsync(ReportKind.DischargeLetter, "fr", CancellationToken.None);
        fallback!.LanguageCode.ShouldBeNull();
    }

    private static ReportTemplate Published(string slug, string? language)
    {
        var t = new ReportTemplate(Guid.NewGuid(), slug, ReportKind.DischargeLetter, slug, language);
        t.AppendVersion("body", "ops", DateTime.UtcNow);
        t.Publish(1);
        return t;
    }

    private sealed class FakeRepo : IPdmsRepository<ReportTemplate, Guid>
    {
        private readonly List<ReportTemplate> _items = new();

        public Task<ReportTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<ReportTemplate?>(_items.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<ReportTemplate>> ListAsync(
            ISpecification<ReportTemplate>? specification = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ReportTemplate>>([.. _items]);

        public Task AddAsync(ReportTemplate aggregate, CancellationToken cancellationToken = default)
        {
            _items.Add(aggregate);
            return Task.CompletedTask;
        }

        public void Update(ReportTemplate aggregate) { }
        public void Remove(ReportTemplate aggregate) => _items.Remove(aggregate);
    }
}
