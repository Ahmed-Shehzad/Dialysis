using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.HIS.Security.Domain.ValueObjects;

namespace Dialysis.HIS.Security.Domain;

/// <summary>
/// Aggregate: a local-account identity used for staff sign-in within HIS while a federated IdP is unavailable
/// or for service accounts. Identity authority itself lives in the Identity module; this aggregate captures the
/// HIS-scoped projection.
/// </summary>
public sealed class LocalUser : AggregateRoot<Guid>
{
    public LoginName LoginName { get; private set; } = null!;
    public string DisplayName { get; private set; } = string.Empty;
    public DateTime RegisteredAtUtc { get; private set; }
    public bool IsActive { get; private set; }

    private LocalUser() { }
    private LocalUser(Guid id) : base(id) { }

    public static LocalUser Register(LoginName loginName, string displayName, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(loginName);
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("LocalUser DisplayName cannot be empty.");
        if (displayName.Length > 256)
            throw new DomainException("LocalUser DisplayName must be 256 chars or fewer.");

        return new LocalUser(Guid.CreateVersion7())
        {
            LoginName = loginName,
            DisplayName = displayName.Trim(),
            RegisteredAtUtc = nowUtc,
            IsActive = true,
        };
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("LocalUser is already deactivated.");
        IsActive = false;
    }
}
