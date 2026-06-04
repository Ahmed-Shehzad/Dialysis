using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.BuildingBlocks.Hipaa.Audit;

/// <summary>
/// Default <see cref="IHipaaAuditContext"/> impl: reads the current user from
/// <see cref="ICurrentUser"/> and the module slug from <see cref="HipaaAuditOptions"/>. Hosts that
/// register both via <c>services.AddHipaaCompliance()</c> + the module's standard
/// <c>AddModuleHost</c> get auditing wired automatically — no per-handler boilerplate.
/// </summary>
public sealed class ModuleScopedHipaaAuditContext : IHipaaAuditContext
{
    private readonly ICurrentUser _currentUser;
    private readonly HipaaAuditOptions _options;
    /// <summary>
    /// Default <see cref="IHipaaAuditContext"/> impl: reads the current user from
    /// <see cref="ICurrentUser"/> and the module slug from <see cref="HipaaAuditOptions"/>. Hosts that
    /// register both via <c>services.AddHipaaCompliance()</c> + the module's standard
    /// <c>AddModuleHost</c> get auditing wired automatically — no per-handler boilerplate.
    /// </summary>
    public ModuleScopedHipaaAuditContext(ICurrentUser currentUser, HipaaAuditOptions options)
    {
        _currentUser = currentUser;
        _options = options;
    }
    public string ModuleSlug => _options.ModuleSlug;

    public string? CurrentUserId => _currentUser.UserId;
}

public sealed class HipaaAuditOptions
{
    /// <summary>Module slug surfaced on every <c>AuditEvent.source.site</c> entry (e.g. <c>his</c>, <c>ehr</c>).</summary>
    public required string ModuleSlug { get; init; }
}
