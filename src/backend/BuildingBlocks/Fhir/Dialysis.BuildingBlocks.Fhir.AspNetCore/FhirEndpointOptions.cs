namespace Dialysis.BuildingBlocks.Fhir.AspNetCore;

public sealed class FhirEndpointOptions
{
    /// <summary>
    /// Route prefix where the FHIR endpoints are mounted. Defaults to <c>/fhir</c>.
    /// Do not nest under <c>/api/v{n}/</c> — FHIR responses must not be wrapped in the
    /// HATEOAS <c>ResourceEnvelope&lt;T&gt;</c> reserved for those routes.
    /// </summary>
    public string BaseUrl { get; set; } = "/fhir";

    /// <summary>
    /// Public base URL of the host, used for <c>CapabilityStatement.url</c> and
    /// <c>Bundle.fullUrl</c> entries. Optional.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// When true, query parameter <c>_format=application/fhir+json</c> overrides the
    /// <c>Accept</c> header per the FHIR specification.
    /// </summary>
    public bool RespectFormatQueryParameter { get; set; } = true;
}
