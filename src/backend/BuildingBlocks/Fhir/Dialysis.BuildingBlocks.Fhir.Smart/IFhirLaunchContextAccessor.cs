using Microsoft.AspNetCore.Http;

namespace Dialysis.BuildingBlocks.Fhir.Smart;

public interface IFhirLaunchContextAccessor
{
    FhirLaunchContext? Current { get; }
}

public sealed record FhirLaunchContext
{
    public FhirLaunchContext(string? Patient, string? Encounter, string? FhirUser, string? Intent)
    {
        this.Patient = Patient;
        this.Encounter = Encounter;
        this.FhirUser = FhirUser;
        this.Intent = Intent;
    }
    public string? Patient { get; init; }
    public string? Encounter { get; init; }
    public string? FhirUser { get; init; }
    public string? Intent { get; init; }
    public void Deconstruct(out string? Patient, out string? Encounter, out string? FhirUser, out string? Intent)
    {
        Patient = this.Patient;
        Encounter = this.Encounter;
        FhirUser = this.FhirUser;
        Intent = this.Intent;
    }
}

public sealed class HttpContextFhirLaunchContextAccessor : IFhirLaunchContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public HttpContextFhirLaunchContextAccessor(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public FhirLaunchContext? Current
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null || !user.Identity?.IsAuthenticated == true)
                return null;
            return new FhirLaunchContext(
                Patient: user.FindFirst("patient")?.Value,
                Encounter: user.FindFirst("encounter")?.Value,
                FhirUser: user.FindFirst("fhirUser")?.Value,
                Intent: user.FindFirst("intent")?.Value);
        }
    }
}
