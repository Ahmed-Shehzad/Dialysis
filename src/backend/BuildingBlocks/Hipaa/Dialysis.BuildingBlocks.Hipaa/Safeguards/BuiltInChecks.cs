using Dialysis.BuildingBlocks.Fhir.Audit;
using Dialysis.BuildingBlocks.Hipaa.Encryption;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Hipaa.Safeguards;

/// <summary>
/// §164.312(a)(2)(iv) — verifies <see cref="IPhiProtector"/> is wired in DI. The PHI value
/// converter is a no-op without one, so a missing protector means PHI is being stored plaintext.
/// </summary>
public sealed class PhiEncryptionEnabledSafeguardCheck : IHipaaSafeguardCheck
{
    private readonly IServiceProvider _services;
    /// <summary>
    /// §164.312(a)(2)(iv) — verifies <see cref="IPhiProtector"/> is wired in DI. The PHI value
    /// converter is a no-op without one, so a missing protector means PHI is being stored plaintext.
    /// </summary>
    public PhiEncryptionEnabledSafeguardCheck(IServiceProvider services) => _services = services;
    public string Id => "encryption-at-rest";
    public string Name => "PHI column encryption is wired";
    public HipaaSafeguardCategory Category => HipaaSafeguardCategory.Technical;
    public string SecurityRuleCitation => "§164.312(a)(2)(iv)";

    public HipaaSafeguardReport Evaluate()
    {
        var protector = _services.GetService<IPhiProtector>();
        if (protector is null)
        {
            return new(HipaaSafeguardStatus.Missing, "No IPhiProtector registered — PHI columns would persist plaintext.");
        }

        try
        {
            const string probe = "hipaa-probe";
            var roundtrip = protector.Decrypt(protector.Encrypt(probe));
            if (!string.Equals(roundtrip, probe, StringComparison.Ordinal))
            {
                return new(HipaaSafeguardStatus.Degraded, "PHI protector round-trip returned a different value.");
            }
            return new(HipaaSafeguardStatus.Active, "DataProtection-backed IPhiProtector resolves and round-trips successfully.");
        }
        catch (Exception ex)
        {
            return new(HipaaSafeguardStatus.Degraded, $"PHI protector threw on probe: {ex.GetType().Name}.");
        }
    }
}

/// <summary>
/// §164.312(b) — verifies <see cref="IAuditEventEmitter"/> is wired. Without it the audit pipeline
/// behaviour falls through to a no-op and PHI accesses leave no trail. Opens a fresh scope so
/// resolving the (Scoped) emitter from the root provider doesn't trip ValidateScopes.
/// </summary>
public sealed class AuditEmitterConfiguredSafeguardCheck : IHipaaSafeguardCheck
{
    private readonly IServiceScopeFactory _scopeFactory;
    /// <summary>
    /// §164.312(b) — verifies <see cref="IAuditEventEmitter"/> is wired. Without it the audit pipeline
    /// behaviour falls through to a no-op and PHI accesses leave no trail. Opens a fresh scope so
    /// resolving the (Scoped) emitter from the root provider doesn't trip ValidateScopes.
    /// </summary>
    public AuditEmitterConfiguredSafeguardCheck(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;
    public string Id => "audit-log-emitter";
    public string Name => "Audit emitter accepts FHIR AuditEvent resources";
    public HipaaSafeguardCategory Category => HipaaSafeguardCategory.Technical;
    public string SecurityRuleCitation => "§164.312(b)";

    public HipaaSafeguardReport Evaluate()
    {
        using var scope = _scopeFactory.CreateScope();
        var emitter = scope.ServiceProvider.GetService<IAuditEventEmitter>();
        return emitter is null
            ? new(HipaaSafeguardStatus.Missing, "No IAuditEventEmitter registered — PHI-access pipeline emits nowhere.")
            : new(HipaaSafeguardStatus.Active, $"Resolved emitter: {emitter.GetType().Name}.");
    }
}

/// <summary>
/// §164.312(a)(2)(ii) — the Data Protection key ring must be persisted (Valkey / file system /
/// Azure / AWS), not the default ephemeral in-memory store, so encrypted PHI survives a host
/// restart. We approximate this by checking the registered <see cref="IXmlRepository"/> type:
/// the default ephemeral provider doesn't register one, so an absent registration ⇒ ephemeral.
/// </summary>
public sealed class DataProtectionKeyRingPersistentSafeguardCheck : IHipaaSafeguardCheck
{
    private readonly IServiceProvider _services;
    /// <summary>
    /// §164.312(a)(2)(ii) — the Data Protection key ring must be persisted (Valkey / file system /
    /// Azure / AWS), not the default ephemeral in-memory store, so encrypted PHI survives a host
    /// restart. We approximate this by checking the registered <see cref="IXmlRepository"/> type:
    /// the default ephemeral provider doesn't register one, so an absent registration ⇒ ephemeral.
    /// </summary>
    public DataProtectionKeyRingPersistentSafeguardCheck(IServiceProvider services) => _services = services;
    public string Id => "data-protection-key-ring";
    public string Name => "Data Protection key ring is persistent";
    public HipaaSafeguardCategory Category => HipaaSafeguardCategory.Technical;
    public string SecurityRuleCitation => "§164.312(a)(2)(ii)";

    public HipaaSafeguardReport Evaluate()
    {
        var opts = _services.GetService<IOptions<KeyManagementOptions>>();
        var repo = opts?.Value.XmlRepository;
        if (repo is null)
        {
            return new(HipaaSafeguardStatus.Degraded,
                "KeyManagementOptions.XmlRepository is not configured — using the default ephemeral key ring; ciphertext does NOT survive host restart.");
        }

        return new(HipaaSafeguardStatus.Active, $"Key ring persisted via {repo.GetType().Name}.");
    }
}

