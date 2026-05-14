using Dialysis.BuildingBlocks.Fhir.Terminology;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Tests.Terminology;

public sealed class MemoryCacheTerminologyDecoratorTests
{
    [Fact]
    public async Task Repeated_Lookup_Hits_Inner_Only_Once_Async()
    {
        var counts = new CallCounts();
        var inner = new CountingTerminologyService(counts);
        var sut = new MemoryCacheTerminologyDecorator(
            inner,
            new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 }),
            Options.Create(new FhirTerminologyOptions { CacheSizeLimit = 100 }));

        await sut.LookupAsync("http://loinc.org", "11506-3", CancellationToken.None);
        await sut.LookupAsync("http://loinc.org", "11506-3", CancellationToken.None);
        await sut.LookupAsync("http://loinc.org", "11506-3", CancellationToken.None);

        counts.LookupCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Disabled_Cache_Always_Calls_Inner_Async()
    {
        var counts = new CallCounts();
        var inner = new CountingTerminologyService(counts);
        var sut = new MemoryCacheTerminologyDecorator(
            inner,
            new MemoryCache(new MemoryCacheOptions { SizeLimit = 1 }),
            Options.Create(new FhirTerminologyOptions { CacheSizeLimit = 0 }));

        await sut.LookupAsync("a", "1", CancellationToken.None);
        await sut.LookupAsync("a", "1", CancellationToken.None);

        counts.LookupCalls.ShouldBe(2);
    }

    private sealed class CallCounts
    {
        public int LookupCalls;
    }

    private sealed class CountingTerminologyService(CallCounts counts) : ITerminologyService
    {
        public ValueTask<Parameters> LookupAsync(string system, string code, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref counts.LookupCalls);
            return new ValueTask<Parameters>(new Parameters());
        }

        public ValueTask<Parameters> ValidateCodeAsync(string valueSetUrl, string code, string? system, CancellationToken cancellationToken)
            => new(new Parameters());

        public ValueTask<Parameters> TranslateAsync(string conceptMapUrl, string sourceSystem, string sourceCode, CancellationToken cancellationToken)
            => new(new Parameters());

        public ValueTask<ValueSet> ExpandAsync(string valueSetUrl, IReadOnlyDictionary<string, string> filters, CancellationToken cancellationToken)
            => new(new ValueSet());
    }
}
