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
public sealed record OutboundBundleDto
{
    /// <summary>
    /// Wire shape for the operator outbound queue view. Status is sent as an enum int so the
    /// SPA's existing tone mapping stays the source of truth; the field names mirror
    /// <see cref="OutboundBundle"/> exactly so backend changes flow forward without a remap.
    /// </summary>
    public OutboundBundleDto(Guid Id,
        Guid PatientId,
        string ResourceType,
        string LogicalId,
        string PartnerId,
        int Status,
        int Attempts,
        DateTime CreatedAtUtc,
        DateTime NextAttemptAtUtc,
        DateTime? DeliveredAtUtc,
        string? LastFailureReason)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.ResourceType = ResourceType;
        this.LogicalId = LogicalId;
        this.PartnerId = PartnerId;
        this.Status = Status;
        this.Attempts = Attempts;
        this.CreatedAtUtc = CreatedAtUtc;
        this.NextAttemptAtUtc = NextAttemptAtUtc;
        this.DeliveredAtUtc = DeliveredAtUtc;
        this.LastFailureReason = LastFailureReason;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string ResourceType { get; init; }
    public string LogicalId { get; init; }
    public string PartnerId { get; init; }
    public int Status { get; init; }
    public int Attempts { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime NextAttemptAtUtc { get; init; }
    public DateTime? DeliveredAtUtc { get; init; }
    public string? LastFailureReason { get; init; }
    public void Deconstruct(out Guid Id, out Guid PatientId, out string ResourceType, out string LogicalId, out string PartnerId, out int Status, out int Attempts, out DateTime CreatedAtUtc, out DateTime NextAttemptAtUtc, out DateTime? DeliveredAtUtc, out string? LastFailureReason)
    {
        Id = this.Id;
        PatientId = this.PatientId;
        ResourceType = this.ResourceType;
        LogicalId = this.LogicalId;
        PartnerId = this.PartnerId;
        Status = this.Status;
        Attempts = this.Attempts;
        CreatedAtUtc = this.CreatedAtUtc;
        NextAttemptAtUtc = this.NextAttemptAtUtc;
        DeliveredAtUtc = this.DeliveredAtUtc;
        LastFailureReason = this.LastFailureReason;
    }
}

public sealed record ListOutboundBundlesQuery : IQuery<IReadOnlyList<OutboundBundleDto>>, IPermissionedCommand
{
    public ListOutboundBundlesQuery(int? StatusFilter, int Take = 50)
    {
        this.StatusFilter = StatusFilter;
        this.Take = Take;
    }
    public string RequiredPermission => HiePermissions.OutboundPublish;
    public int? StatusFilter { get; init; }
    public int Take { get; init; }
    public void Deconstruct(out int? StatusFilter, out int Take)
    {
        StatusFilter = this.StatusFilter;
        Take = this.Take;
    }
}
