using Dialysis.BuildingBlocks.Hipaa.Audit;
using Dialysis.BuildingBlocks.Hipaa.Encryption;
using Dialysis.BuildingBlocks.Hipaa.Safeguards;
using Dialysis.BuildingBlocks.Intercessor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Hipaa;

/// <summary>
/// Single-line wire-up for the HIPAA Security Rule scaffolding. Modules call
/// <c>services.AddHipaaCompliance()</c> in their composition root and inherit:
///  • PHI column encryption via <see cref="Encryption.IPhiProtector"/>
///  • Auto-audit pipeline behaviour for every <c>[PhiAccess]</c>-marked command/query
///  • The safeguard registry used by the <c>/admin/hipaa/safeguards</c> dashboard
///
/// Each pillar is also addressable individually so tests can compose narrower setups.
/// </summary>
public static class HipaaServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHipaaPhiEncryption()
        {
            // Self-sufficient registration: if the host already wired DataProtection (e.g. via
            // Valkey's PersistKeysToStackExchangeRedis), AddDataProtection is idempotent and the
            // earlier persistent key-ring registration wins. If nothing wired it, we fall back
            // to the ephemeral key ring so DI validation succeeds — the
            // <c>data-protection-key-ring</c> safeguard flags this as Degraded for the operator.
            services.AddDataProtection();
            services.TryAddSingleton<IPhiProtector, DataProtectionPhiProtector>();
            return services;
        }

        /// <summary>
        /// Registers <see cref="HipaaAuditingBehavior{TRequest,TResponse}"/> as an open-generic
        /// <see cref="IPipelineBehavior{TRequest,TResponse}"/>. Intercessor invokes it for every
        /// request and the behaviour itself skips when the request type lacks <c>[PhiAccess]</c>,
        /// so the pipeline cost is negligible for non-PHI requests.
        /// </summary>
        public IServiceCollection AddHipaaAuditPipeline()
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HipaaAuditingBehavior<,>));
            return services;
        }

        /// <summary>
        /// Registers the safeguard registry and the four built-in checks. Modules can register
        /// additional <see cref="Safeguards.IHipaaSafeguardCheck"/> singletons before or after this
        /// call — <see cref="Safeguards.HipaaSafeguardRegistry"/> picks them up via the DI enumerable.
        /// </summary>
        public IServiceCollection AddHipaaSafeguards()
        {
            services.AddSingleton<IHipaaSafeguardCheck, PhiEncryptionEnabledSafeguardCheck>();
            services.AddSingleton<IHipaaSafeguardCheck, AuditEmitterConfiguredSafeguardCheck>();
            services.AddSingleton<IHipaaSafeguardCheck, DataProtectionKeyRingPersistentSafeguardCheck>();
            services.TryAddSingleton<HipaaSafeguardRegistry>();
            return services;
        }

        /// <summary>
        /// Registers all three pillars in one call. The host must additionally:
        ///  • Register <see cref="Dialysis.Module.Contracts.Authorization.ICurrentUser"/>
        ///    (handled by <c>AddModuleHost</c>).
        ///  • Register <see cref="HipaaAuditOptions"/> with the module's slug.
        ///  • Register <see cref="Fhir.Audit.IAuditEventEmitter"/> (call <c>services.AddFhirAudit()</c>).
        /// </summary>
        public IServiceCollection AddHipaaCompliance(string moduleSlug)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleSlug);
            services.AddSingleton(new HipaaAuditOptions { ModuleSlug = moduleSlug });
            services.TryAddScoped<IHipaaAuditContext, ModuleScopedHipaaAuditContext>();
            return services
                .AddHipaaPhiEncryption()
                .AddHipaaAuditPipeline()
                .AddHipaaSafeguards();
        }
    }
}
