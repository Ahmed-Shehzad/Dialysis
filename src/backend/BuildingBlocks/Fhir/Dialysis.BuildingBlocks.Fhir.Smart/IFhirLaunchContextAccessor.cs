using Microsoft.AspNetCore.Http;

namespace Dialysis.BuildingBlocks.Fhir.Smart;

public interface IFhirLaunchContextAccessor
{
    FhirLaunchContext? Current { get; }
}

public sealed record FhirLaunchContext(string? Patient, string? Encounter, string? FhirUser, string? Intent);

public sealed class HttpContextFhirLaunchContextAccessor(IHttpContextAccessor httpContextAccessor) : IFhirLaunchContextAccessor
{
    public FhirLaunchContext? Current
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
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
