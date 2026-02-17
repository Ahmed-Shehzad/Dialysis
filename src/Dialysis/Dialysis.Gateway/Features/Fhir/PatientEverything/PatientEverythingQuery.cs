using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Fhir.PatientEverything;

/// <summary>
/// Query for Patient $everything bundle. Returns FHIR JSON or null if patient not found.
/// </summary>
public sealed record PatientEverythingQuery(string BaseUrl, string PatientId) : IQuery<PatientEverythingResult?>;

public sealed record PatientEverythingResult(string FhirBundleJson);
