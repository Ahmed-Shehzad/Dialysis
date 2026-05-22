namespace Dialysis.SmartConnect.Pdq;

/// <summary>
/// Port that resolves a parsed <see cref="PdqCriteria"/> into zero or more
/// <see cref="PdqMatch"/> rows. The Core layer holds only this contract; the production
/// implementation lives in <c>Dialysis.SmartConnect.Api</c> and HTTP-calls EHR's existing
/// patient-search endpoint, treating EHR as an external service (per the module-boundary
/// invariant — SmartConnect must not take a direct project dependency on EHR internals).
/// </summary>
public interface IPatientDemographicsResolver
{
    Task<IReadOnlyList<PdqMatch>> ResolveAsync(PdqCriteria criteria, CancellationToken cancellationToken = default);
}
