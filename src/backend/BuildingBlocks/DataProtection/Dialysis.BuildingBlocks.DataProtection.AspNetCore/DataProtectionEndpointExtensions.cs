using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;
using Dialysis.BuildingBlocks.DataProtection.Ropa;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.BuildingBlocks.DataProtection.AspNetCore;

/// <summary>
/// Maps the GDPR / BDSG / PDSG endpoints into a host. Call from every module's
/// <c>Program.cs</c> alongside the existing <c>MapHipaaSafeguardsEndpoint()</c>.
/// </summary>
public static class DataProtectionEndpointExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>Registers data-protection routes (RoPA, data-subject rights).</summary>
        public IEndpointRouteBuilder MapEuDataProtectionRoutes()
        {
            var admin = endpoints.MapGroup("/admin/data-protection")
                .WithTags("EU Data Protection");

            admin.MapGet("/ropa", (IRopaGenerator generator) =>
                {
                    var doc = generator.Generate();
                    return Results.Ok(doc);
                })
                .WithName("DataProtection_Ropa");

            var subjects = endpoints.MapGroup("/api/v1.0/data-subject-rights")
                .WithTags("EU Data Protection — Data Subject Rights");

            subjects.MapGet(
                    "/{patientId:guid}/export",
                    async (
                        Guid patientId,
                        IDataSubjectRightsService service,
                        CancellationToken ct) =>
                    {
                        var export = await service.ExportAsync(patientId, ct).ConfigureAwait(false);
                        return Results.Ok(export);
                    })
                .WithName("DataSubjectRights_Export");

            subjects.MapPost(
                    "/{patientId:guid}/erasure-request",
                    async (
                        Guid patientId,
                        ErasureRequestBody body,
                        IDataSubjectRightsService service,
                        CancellationToken ct) =>
                    {
                        var id = await service.RequestErasureAsync(
                            patientId, body.RequestedBy, body.Reason, ct)
                            .ConfigureAwait(false);
                        return Results.Accepted($"/data-subject-rights/{patientId}/requests/{id}", new { requestId = id });
                    })
                .WithName("DataSubjectRights_Erasure");

            subjects.MapPost(
                    "/{patientId:guid}/restriction",
                    async (
                        Guid patientId,
                        RestrictionRequestBody body,
                        IDataSubjectRightsService service,
                        CancellationToken ct) =>
                    {
                        var id = await service.RequestRestrictionAsync(
                            patientId, body.RequestedBy, body.Reason, ct)
                            .ConfigureAwait(false);
                        return Results.Accepted($"/data-subject-rights/{patientId}/requests/{id}", new { requestId = id });
                    })
                .WithName("DataSubjectRights_Restriction");

            return endpoints;
        }
    }
}

public sealed class ErasureRequestBody
{
    public string RequestedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public sealed class RestrictionRequestBody
{
    public string RequestedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
