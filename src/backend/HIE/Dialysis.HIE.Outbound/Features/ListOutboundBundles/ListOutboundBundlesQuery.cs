using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Outbound.Features.ListOutboundBundles;

/// <summary>
/// Wire shape for the operator outbound queue view. Status is sent as an enum int so the
/// SPA's existing tone mapping stays the source of truth; the field names mirror
/// <see cref="OutboundBundle"/> exactly so backend changes flow forward without a remap.
/// </summary>
public sealed record OutboundBundleDto(
    Guid Id,
    Guid PatientId,
    string ResourceType,
    string LogicalId,
    string PartnerId,
    int Status,
    int Attempts,
    DateTime CreatedAtUtc,
    DateTime NextAttemptAtUtc,
    DateTime? DeliveredAtUtc,
    string? LastFailureReason);

public sealed record ListOutboundBundlesQuery(int? StatusFilter, int Take = 50)
    : IQuery<IReadOnlyList<OutboundBundleDto>>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.OutboundPublish;
}
