using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Terminology;

public interface ITerminologyService
{
    ValueTask<Parameters> LookupAsync(string system, string code, CancellationToken cancellationToken);

    ValueTask<Parameters> ValidateCodeAsync(string valueSetUrl, string code, string? system, CancellationToken cancellationToken);

    ValueTask<Parameters> TranslateAsync(string conceptMapUrl, string sourceSystem, string sourceCode, CancellationToken cancellationToken);

    ValueTask<ValueSet> ExpandAsync(string valueSetUrl, IReadOnlyDictionary<string, string> filters, CancellationToken cancellationToken);
}
