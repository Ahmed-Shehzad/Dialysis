namespace Dialysis.Module.Contracts.Authorization;

/// <summary>
/// Per-request principal projection used by handlers, the audit interceptor, and pipeline behaviors.
/// Hosts supply an implementation that reads from the active transport (HTTP, message envelope, etc.).
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }

    IReadOnlyCollection<string> Permissions { get; }
}
