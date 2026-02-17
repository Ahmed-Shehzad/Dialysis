namespace Dialysis.Gateway.Services;

/// <summary>
/// Builds FHIR bundles from patient data. Single responsibility: FHIR bundle construction.
/// </summary>
public interface IFhirBundleBuilder
{
    string BuildPatientEverythingBundle(PatientDataAggregate data, string baseUrl);
    string BuildEhrPushTransactionBundle(PatientDataAggregate data, string baseUrl);
}
