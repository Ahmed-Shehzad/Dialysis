using Dialysis.CQRS.Commands;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Outbound.Features.RetryOutboundBundle;

/// <summary>
/// Operator-driven retry of a <c>Failed</c> outbound bundle. Resets the status to
/// <c>Pending</c> with an immediate <c>NextAttemptAtUtc</c> so the dispatcher picks it up
/// on its next tick. The attempt counter is preserved — operators are explicitly accepting
/// the previous failures as audit history, not erasing them. No-ops on already-delivered
/// bundles.
/// </summary>
public sealed record RetryOutboundBundleCommand : ICommand, IPermissionedCommand
{
    /// <summary>
    /// Operator-driven retry of a <c>Failed</c> outbound bundle. Resets the status to
    /// <c>Pending</c> with an immediate <c>NextAttemptAtUtc</c> so the dispatcher picks it up
    /// on its next tick. The attempt counter is preserved — operators are explicitly accepting
    /// the previous failures as audit history, not erasing them. No-ops on already-delivered
    /// bundles.
    /// </summary>
    public RetryOutboundBundleCommand(Guid BundleId) => this.BundleId = BundleId;
    public string RequiredPermission => HiePermissions.OutboundPublish;
    public Guid BundleId { get; init; }
    public void Deconstruct(out Guid bundleId) => bundleId = this.BundleId;
}
