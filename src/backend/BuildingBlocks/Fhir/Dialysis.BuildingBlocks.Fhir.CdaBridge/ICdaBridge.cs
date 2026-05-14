using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.CdaBridge;

public interface ICdaToFhirMapper
{
    Bundle Map(string cdaXml);
}

public interface IFhirToCdaMapper
{
    string Map(Bundle bundle);
}
