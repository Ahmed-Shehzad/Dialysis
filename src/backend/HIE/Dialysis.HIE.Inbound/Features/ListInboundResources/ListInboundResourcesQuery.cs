using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Inbound.Features.ListInboundResources;

/// <summary>
/// Wire shape for the operator inbound feed. Mirrors the
/// <c>ReceivedResource</c> domain row; <c>FhirJson</c> is intentionally excluded — the
/// admin board doesn't render full payloads, only the metadata strip.
/// </summary>
public sealed record InboundResourceDto(
    Guid Id,
    string PartnerId,
    string ResourceType,
    string LogicalId,
    DateTime ReceivedAtUtc,
    string? ValidationOutcome);

public sealed record ListInboundResourcesQuery(string? PartnerId, int Take = 50)
    : IQuery<IReadOnlyList<InboundResourceDto>>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.InboundReceive;
}
