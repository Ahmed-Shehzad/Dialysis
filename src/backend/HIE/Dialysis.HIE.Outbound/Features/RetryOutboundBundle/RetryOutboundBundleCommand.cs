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
public sealed record RetryOutboundBundleCommand(Guid BundleId)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.OutboundPublish;
}
