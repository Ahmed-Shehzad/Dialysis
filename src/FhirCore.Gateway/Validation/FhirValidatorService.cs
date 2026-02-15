using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Firely.Fhir.Validation;

namespace FhirCore.Gateway.Validation;

public sealed class FhirValidatorService
{
    private readonly Validator _validator;
    private readonly FhirJsonDeserializer _deserializer = new();

    public FhirValidatorService(
        IAsyncResourceResolver resolver,
        ICodeValidationTerminologyService terminologyService)
    {
        _validator = new Validator(resolver, terminologyService, null, null, null);
    }

    public OperationOutcome Validate(Resource instance, string? profile = null)
    {
        return _validator.Validate(instance, profile ?? string.Empty);
    }

    public (Resource? Resource, string? Error) TryParseResource(string json)
    {
        try
        {
            var resource = _deserializer.Deserialize<Resource>(json);
            return (resource, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }
}
