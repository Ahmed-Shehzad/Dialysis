using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Inbound.Features.ListInboundResources;

/// <summary>
/// Wire shape for the operator inbound feed. Mirrors the
/// <c>ReceivedResource</c> domain row; <c>FhirJson</c> is intentionally excluded — the
/// admin board doesn't render full payloads, only the metadata strip.
/// </summary>
public sealed record InboundResourceDto
{
    /// <summary>
    /// Wire shape for the operator inbound feed. Mirrors the
    /// <c>ReceivedResource</c> domain row; <c>FhirJson</c> is intentionally excluded — the
    /// admin board doesn't render full payloads, only the metadata strip.
    /// </summary>
    public InboundResourceDto(Guid Id,
        string PartnerId,
        string ResourceType,
        string LogicalId,
        DateTime ReceivedAtUtc,
        string? ValidationOutcome)
    {
        this.Id = Id;
        this.PartnerId = PartnerId;
        this.ResourceType = ResourceType;
        this.LogicalId = LogicalId;
        this.ReceivedAtUtc = ReceivedAtUtc;
        this.ValidationOutcome = ValidationOutcome;
    }
    public Guid Id { get; init; }
    public string PartnerId { get; init; }
    public string ResourceType { get; init; }
    public string LogicalId { get; init; }
    public DateTime ReceivedAtUtc { get; init; }
    public string? ValidationOutcome { get; init; }
    public void Deconstruct(out Guid Id, out string PartnerId, out string ResourceType, out string LogicalId, out DateTime ReceivedAtUtc, out string? ValidationOutcome)
    {
        Id = this.Id;
        PartnerId = this.PartnerId;
        ResourceType = this.ResourceType;
        LogicalId = this.LogicalId;
        ReceivedAtUtc = this.ReceivedAtUtc;
        ValidationOutcome = this.ValidationOutcome;
    }
}

public sealed record ListInboundResourcesQuery : IQuery<IReadOnlyList<InboundResourceDto>>, IPermissionedCommand
{
    public ListInboundResourcesQuery(string? PartnerId, int Take = 50)
    {
        this.PartnerId = PartnerId;
        this.Take = Take;
    }
    public string RequiredPermission => HiePermissions.InboundReceive;
    public string? PartnerId { get; init; }
    public int Take { get; init; }
    public void Deconstruct(out string? PartnerId, out int Take)
    {
        PartnerId = this.PartnerId;
        Take = this.Take;
    }
}
