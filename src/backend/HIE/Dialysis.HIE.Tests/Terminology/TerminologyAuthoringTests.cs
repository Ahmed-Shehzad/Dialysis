using Dialysis.BuildingBlocks.Fhir.Terminology;
using Dialysis.HIE.Inbound.Terminology;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Terminology;

/// <summary>
/// Coverage for terminology authoring: the catalog overlay seam (authored resources become servable)
/// and the upsert handler's fail-closed FhirJson validation.
/// </summary>
public sealed class TerminologyAuthoringTests
{
    private const string ValueSetUrl = "https://dialysis.local/fhir/ValueSet/authored-test";
    private const string SystemUrl = "https://dialysis.local/fhir/CodeSystem/authored-test";

    private static ValueSet AuthoredValueSet() => new()
    {
        Url = ValueSetUrl,
        Version = "1.0.0",
        Name = "AuthoredTest",
        Status = PublicationStatus.Active,
        Compose = new ValueSet.ComposeComponent
        {
            Include =
            [
                new ValueSet.ConceptSetComponent
                {
                    System = SystemUrl,
                    Concept = [new ValueSet.ConceptReferenceComponent { Code = "C1", Display = "Concept One" }],
                },
            ],
        },
    };

    [Fact]
    public void Register_Overlays_An_Authored_Value_Set_Into_The_Governance_Listing()
    {
        var catalog = new DialysisTerminologyCatalog();
        var before = catalog.Resources.Count;

        catalog.Register(AuthoredValueSet());

        catalog.Resources.Count.ShouldBe(before + 1);
        catalog.Resources.ShouldContain(r => r.ResourceType == "ValueSet" && r.Url == ValueSetUrl);
    }

    [Fact]
    public async Task Overlaid_Value_Set_Validates_Its_Codes_Async()
    {
        var catalog = new DialysisTerminologyCatalog();
        catalog.Register(AuthoredValueSet());

        var result = await catalog.Service.ValidateCodeAsync(ValueSetUrl, "C1", SystemUrl, CancellationToken.None);

        var resultParam = result.Parameter.Single(p => p.Name == "result");
        ((FhirBoolean)resultParam.Value!).Value.ShouldBe(true);
    }

    [Fact]
    public async Task Upsert_Rejects_Fhir_Json_Whose_Type_Does_Not_Match_The_Declared_Type_Async()
    {
        var handler = new UpsertAuthoredTerminologyCommandHandler(new FakeRepo(), TimeProvider.System);

        // Body is a ValueSet, but ResourceType says CodeSystem → fail closed.
        var command = new UpsertAuthoredTerminologyCommand(
            ResourceType: "CodeSystem",
            Url: ValueSetUrl,
            Version: "1.0.0",
            Status: "active",
            Name: "Mismatch",
            FhirJson: $$"""{"resourceType":"ValueSet","url":"{{ValueSetUrl}}","status":"active"}""",
            UpdatedBy: "tester");

        await Should.ThrowAsync<ArgumentException>(() => handler.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task Upsert_Persists_A_Well_Formed_Resource_Async()
    {
        var repo = new FakeRepo();
        var handler = new UpsertAuthoredTerminologyCommandHandler(repo, TimeProvider.System);

        var command = new UpsertAuthoredTerminologyCommand(
            ResourceType: "ValueSet",
            Url: ValueSetUrl,
            Version: "1.0.0",
            Status: "active",
            Name: "AuthoredTest",
            FhirJson: $$"""{"resourceType":"ValueSet","url":"{{ValueSetUrl}}","status":"active"}""",
            UpdatedBy: "tester");

        var id = await handler.HandleAsync(command, CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        repo.Added.ShouldHaveSingleItem().Url.ShouldBe(ValueSetUrl);
        repo.Saved.ShouldBeTrue();
    }

    private sealed class FakeRepo : IAuthoredTerminologyRepository
    {
        public List<AuthoredTerminologyResource> Added { get; } = [];
        public bool Saved { get; private set; }

        public void Add(AuthoredTerminologyResource resource) => Added.Add(resource);
        public void Remove(AuthoredTerminologyResource resource) => Added.Remove(resource);
        public Task<AuthoredTerminologyResource?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Added.FirstOrDefault(r => r.Id == id));
        public Task<AuthoredTerminologyResource?> FindByUrlVersionAsync(string url, string version, CancellationToken cancellationToken) =>
            Task.FromResult(Added.FirstOrDefault(r => r.Url == url && r.Version == version));
        public Task<IReadOnlyList<AuthoredTerminologyResource>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AuthoredTerminologyResource>>(Added);
        public Task<IReadOnlyList<AuthoredTerminologyResource>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AuthoredTerminologyResource>>([.. Added.Where(r => r.Status == "active")]);
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saved = true;
            return Task.CompletedTask;
        }
    }
}
