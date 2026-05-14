using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir;

/// <summary>
/// Source of FHIR resource entries advertised in <c>CapabilityStatement.rest.resource[]</c>.
/// Default implementation reflects over registered <see cref="IFhirReader{TResource}"/> and
/// <see cref="IFhirSearcher{TResource}"/> services.
/// </summary>
public interface IFhirCapabilityProvider
{
    IReadOnlyList<CapabilityStatement.ResourceComponent> DescribeResources();

    string FhirVersion => "4.0.1";
}
