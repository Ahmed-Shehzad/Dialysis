namespace Dialysis.Identity.Provisioning.Domain;

public enum UserAccountStatus
{
    Provisioned = 1,
    Deactivated = 2,
}

/// <summary>
/// Identity module's authoritative record of an end user. The <see cref="Subject"/> is the
/// stable identifier issued by the upstream OIDC provider (e.g. Keycloak's <c>sub</c> claim).
/// </summary>
public sealed class UserAccount
{
    public Guid Id { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public UserAccountStatus Status { get; set; } = UserAccountStatus.Provisioned;
}
