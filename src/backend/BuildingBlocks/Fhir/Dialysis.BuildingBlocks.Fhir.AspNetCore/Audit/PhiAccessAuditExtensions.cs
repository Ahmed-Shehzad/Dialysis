using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;

/// <summary>
/// Wires <see cref="PhiAccessAuditFilter"/> into the MVC pipeline. Adds the filter as a
/// global filter so every controller action tagged with <see cref="PhiAccessAttribute"/>
/// gets covered automatically — modules don't have to remember to opt in per-controller.
/// </summary>
public static class PhiAccessAuditExtensions
{
    public static IMvcBuilder AddPhiAccessAuditing(this IMvcBuilder mvc)
    {
        ArgumentNullException.ThrowIfNull(mvc);
        mvc.Services.AddSingleton<PhiAccessAuditFilter>();
        mvc.AddMvcOptions(options =>
        {
            options.Filters.AddService<PhiAccessAuditFilter>();
        });
        return mvc;
    }
}
