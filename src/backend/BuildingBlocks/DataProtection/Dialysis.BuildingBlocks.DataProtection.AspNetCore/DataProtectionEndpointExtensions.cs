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

            // --- Operator-facing erasure pipeline ---------------------------
            // Once a subject files an erasure request, the operator (DPO) reviews each row
            // and either approves it (which runs every registered IPatientEraser) or
            // rejects it (legal-hold applies / duplicate / etc.). Both branches persist the
            // audit row so a regulator can verify the decision end-to-end.

            subjects.MapGet(
                    "/erasure/requests",
                    async (
                        IDataSubjectRightsService service,
                        CancellationToken ct,
                        int take = 50) =>
                    {
                        var rows = await service.ListPendingErasureRequestsAsync(take, ct)
                            .ConfigureAwait(false);
                        return Results.Ok(rows);
                    })
                .WithName("DataSubjectRights_ListPendingErasureRequests");

            subjects.MapPost(
                    "/erasure/{requestId:guid}/approve",
                    async (
                        Guid requestId,
                        ErasureDecisionBody body,
                        IDataSubjectRightsService service,
                        CancellationToken ct) =>
                    {
                        var executed = await service.ApproveErasureRequestAsync(
                            requestId, body.DecidedBy, ct).ConfigureAwait(false);
                        return Results.Ok(executed);
                    })
                .WithName("DataSubjectRights_ApproveErasure");

            subjects.MapPost(
                    "/erasure/{requestId:guid}/reject",
                    async (
                        Guid requestId,
                        ErasureRejectionBody body,
                        IDataSubjectRightsService service,
                        CancellationToken ct) =>
                    {
                        var rejected = await service.RejectErasureRequestAsync(
                            requestId, body.DecidedBy, body.Reason, ct).ConfigureAwait(false);
                        return Results.Ok(rejected);
                    })
                .WithName("DataSubjectRights_RejectErasure");

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

public sealed class ErasureDecisionBody
{
    public string DecidedBy { get; set; } = string.Empty;
}

public sealed class ErasureRejectionBody
{
    public string DecidedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
