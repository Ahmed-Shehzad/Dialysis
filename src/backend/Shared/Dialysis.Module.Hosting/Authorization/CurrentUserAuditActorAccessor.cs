using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Module.Hosting.Authorization;

/// <summary>
/// Bridges <see cref="ICurrentUser"/> into <see cref="IAuditActorAccessor"/> so the
/// audit interceptor stamps the same identity that handler-level authorization sees.
/// </summary>
internal sealed class CurrentUserAuditActorAccessor : IAuditActorAccessor
{
    private readonly ICurrentUser _currentUser;
    /// <summary>
    /// Bridges <see cref="ICurrentUser"/> into <see cref="IAuditActorAccessor"/> so the
    /// audit interceptor stamps the same identity that handler-level authorization sees.
    /// </summary>
    public CurrentUserAuditActorAccessor(ICurrentUser currentUser) => _currentUser = currentUser;
    public string? ActorId => _currentUser.UserId;
}
