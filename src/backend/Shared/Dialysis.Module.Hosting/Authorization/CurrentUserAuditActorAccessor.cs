using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Module.Hosting.Authorization;

/// <summary>
/// Bridges <see cref="ICurrentUser"/> into <see cref="IAuditActorAccessor"/> so the
/// audit interceptor stamps the same identity that handler-level authorization sees.
/// </summary>
internal sealed class CurrentUserAuditActorAccessor(ICurrentUser currentUser) : IAuditActorAccessor
{
    public string? ActorId => currentUser.UserId;
}
