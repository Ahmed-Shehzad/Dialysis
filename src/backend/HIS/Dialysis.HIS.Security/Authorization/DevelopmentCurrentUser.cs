using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Security.Authorization;

/// <summary>
/// Grants all HIS permissions for local development / tests. Replace with real claims in production.
/// </summary>
public sealed class DevelopmentCurrentUser : ICurrentUser
{
    public string? UserId => "dev-user";

    public IReadOnlyCollection<string> Permissions { get; } = HisPermissions.All.ToArray();
}
