namespace Dialysis.AppHost;

/// <summary>
/// Names of the independently deployable bounded-context units this AppHost can publish, plus the
/// membership logic that decides which resources stay in-model for a given unit. Read from
/// configuration key <see cref="EnvVarName"/> (which the NUKE <c>PublishKubernetesUnit</c> /
/// <c>PublishAllKubernetesUnits</c> targets thread through as an env var on the <c>dotnet run</c>
/// invocation that drives the k8s publisher).
///
/// The variable is honoured in <b>publish mode only</b> and only by the <b>k8s</b> publisher:
/// the dev F5 loop and the compose publisher always run the full topology, and when the variable
/// is absent the publish output is byte-identical to the classic full-stack artifacts (the
/// unit-filtering paths simply never execute).
///
/// Every unit chart installs into the same namespace as the full chart
/// (<c>dialysis-&lt;env&gt;</c>) so units interoperate over stable in-cluster Service DNS names —
/// the cross-unit contract captured by the <c>*Default</c> members below and documented in
/// <c>deploy/charts/units/README.md</c>.
/// </summary>
public static class DeploymentUnit
{
    /// <summary>HIS bounded context: his-api + his-bff + his-web + postgres-his.</summary>
    public const string His = "his";

    /// <summary>EHR bounded context: ehr-api + ehr-bff + ehr-web + postgres-ehr.</summary>
    public const string Ehr = "ehr";

    /// <summary>PDMS bounded context: pdms-api + pdms-bff + pdms-web + postgres-pdms (TimescaleDB).</summary>
    public const string Pdms = "pdms";

    /// <summary>SmartConnect bounded context: smartconnect-api + smartconnect-bff + smartconnect-web + postgres-smartconnect.</summary>
    public const string SmartConnect = "smartconnect";

    /// <summary>HIE bounded context: hie-api + hie-bff + hie-web + postgres-hie.</summary>
    public const string Hie = "hie";

    /// <summary>Lab bounded context (headless — no BFF/SPA): lab-api + postgres-lab.</summary>
    public const string Lab = "lab";

    /// <summary>
    /// Identity context: Keycloak + the identity BFF (OIDC handshake) + the admin BFF + the
    /// identity-web admin console. (There is no separate "Identity API"/"Identity Postgres" in the
    /// resource graph — Keycloak runs <c>KC_DB=dev-mem</c> and the BFFs' Hangfire storage rides on
    /// the HIS/HIE module databases, which become external connection strings in this unit.)
    /// </summary>
    public const string Identity = "identity";

    /// <summary>Patient portal context: portal-bff + patient-portal-web (domain lives in EHR/HIS).</summary>
    public const string Portal = "portal";

    /// <summary>Platform unit: the edge gateway + RabbitMQ + Valkey (and the namespace-wide NetworkPolicies + Ingress).</summary>
    public const string Platform = "platform";

    /// <summary>Configuration key the AppHost reads to pick the published unit (unset = full topology).</summary>
    public const string EnvVarName = "DIALYSIS_DEPLOY_UNIT";

    /// <summary>Every publishable unit, in install order (platform first, then identity, then the contexts).</summary>
    public static readonly IReadOnlyList<string> All =
        [Platform, Identity, His, Ehr, Pdms, SmartConnect, Hie, Lab, Portal];

    /// <summary>
    /// Normalizes the raw configuration value: <c>null</c>/empty/whitespace mean "no unit — full
    /// topology"; anything else must be one of <see cref="All"/> (case-insensitive) or we throw,
    /// so a typo can never silently publish the full stack as if it were a unit.
    /// </summary>
    public static string? NormalizeOrThrow(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        var unit = value.Trim().ToLowerInvariant();
        return All.Contains(unit)
            ? unit
            : throw new InvalidOperationException(
                $"Unknown {EnvVarName} '{value}'. Expected one of: {string.Join(", ", All)}.");
    }

    /// <summary><c>true</c> when RabbitMQ + Valkey are in-model (full topology or the platform unit).</summary>
    public static bool IncludesSharedInfra(string? unit) => unit is null or Platform;

    /// <summary><c>true</c> when the Keycloak container is in-model (full topology or the identity unit).</summary>
    public static bool IncludesKeycloak(string? unit) => unit is null or Identity;

    /// <summary><c>true</c> when the edge gateway is in-model (full topology or the platform unit).</summary>
    public static bool IncludesGateway(string? unit) => unit is null or Platform;

    /// <summary><c>true</c> when the named bounded-context unit's resources are in-model.</summary>
    public static bool IncludesContext(string? unit, string contextUnit) => unit is null || unit == contextUnit;

    // --- Cross-unit in-cluster DNS contract -------------------------------------------------
    // Aspire's k8s publisher names every Service "<resource>-service" (release-name-free), so as
    // long as all units install into the same namespace these names are stable regardless of
    // which Helm release owns them. The defaults below are emitted into each unit chart's
    // values.yaml, where operators override them when their cluster shape differs.

    /// <summary>
    /// In-cluster default for the Keycloak realm authority. The identity unit's chart (like the
    /// full chart) emits a Service named <c>keycloak-service</c> with port 8081 → targetPort 8080,
    /// so cross-unit consumers must dial the Service port (8081).
    /// </summary>
    public const string KeycloakAuthorityDefault = "http://keycloak-service:8081/realms/dialysis";

    /// <summary>In-cluster default for the platform unit's browser-facing gateway Service.</summary>
    public const string GatewayAddressDefault = "http://gateway-service:9090";

    /// <summary>
    /// In-cluster default for the platform unit's RabbitMQ broker. CHANGE-ME marks the credential
    /// an operator must override (values.yaml) with the platform release's real password — a
    /// resolvable default is required so the publish pipeline can render the chart at all.
    /// </summary>
#pragma warning disable S2068 // CHANGE-ME placeholders, not credentials: the KeycloakSecretGuard
    // convention; operators must override these values per cluster.
    public const string RabbitMqConnectionDefault = "amqp://guest:CHANGE-ME@rabbitmq-service:5672";

    /// <summary>In-cluster default for the platform unit's Valkey. Same CHANGE-ME convention.</summary>
    public const string ValkeyConnectionDefault = "valkey-service:6379,password=CHANGE-ME";

    /// <summary>
    /// In-cluster default for a sibling unit's module Postgres (Hangfire storage for the
    /// identity/portal BFFs). Same CHANGE-ME convention.
    /// </summary>
    public static string ModulePostgresConnectionDefault(string serverResourceName, string databaseName) =>
        $"Host={serverResourceName}-service;Port=5432;Username=postgres;Password=CHANGE-ME;Database={databaseName}";
#pragma warning restore S2068

    /// <summary>
    /// In-cluster address of a module API published by another unit. Module API Services expose
    /// the ASP.NET container port (8080) one-to-one.
    /// </summary>
    public static string ModuleApiAddress(string resourceName) => $"http://{resourceName}-service:8080";

    /// <summary>
    /// The platform unit's gateway ReverseProxy cluster destinations — one stable in-cluster
    /// Service DNS default per cluster id in the gateway's <c>appsettings.json</c>. Each lands in
    /// the unit chart's values.yaml as
    /// <c>ReverseProxy__Clusters__&lt;id&gt;__Destinations__d1__Address</c>, overridable per
    /// cluster at install time. BFF Services expose their pinned ports (identity 5275, contexts
    /// 5301–5307); web Services expose the SPA dev ports (5331–5337); Keycloak its Service port
    /// (8081). Note the two web clusters whose id differs from the resource/Service name
    /// (admin-web → identity-web, portal-web → patient-portal-web).
    /// </summary>
    public static readonly IReadOnlyList<(string ClusterId, string Address)> GatewayClusterDefaults =
    [
        ("identity", "http://identity-bff-service:5275/"),
        ("his-bff", "http://his-bff-service:5301/"),
        ("ehr-bff", "http://ehr-bff-service:5302/"),
        ("pdms-bff", "http://pdms-bff-service:5303/"),
        ("smartconnect-bff", "http://smartconnect-bff-service:5304/"),
        ("hie-bff", "http://hie-bff-service:5305/"),
        ("admin-bff", "http://admin-bff-service:5306/"),
        ("portal-bff", "http://portal-bff-service:5307/"),
        ("his-web", "http://his-web-service:5331/"),
        ("ehr-web", "http://ehr-web-service:5332/"),
        ("pdms-web", "http://pdms-web-service:5333/"),
        ("smartconnect-web", "http://smartconnect-web-service:5334/"),
        ("hie-web", "http://hie-web-service:5335/"),
        ("admin-web", "http://identity-web-service:5336/"),
        ("portal-web", "http://patient-portal-web-service:5337/"),
        ("keycloak", "http://keycloak-service:8081/"),
    ];
}

/// <summary>
/// Conditional fluent helpers for unit-mode publishing: a cross-unit dependency has no in-model
/// resource to wait on, so <c>WaitFor</c> must be skipped for it. Routing every wait through
/// <see cref="WaitForIfPresent{T}"/> keeps the full-topology call sequence (and therefore the
/// committed full-stack artifacts) byte-identical — when the dependency is present the call is
/// exactly the <c>WaitFor</c> the AppHost always made.
/// </summary>
public static class ConditionalResourceBuilderExtensions
{
    /// <summary>Waits for <paramref name="dependency"/> only when it is in-model (non-null).</summary>
    public static IResourceBuilder<T> WaitForIfPresent<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<IResource>? dependency)
        where T : IResourceWithWaitSupport
        => dependency is null ? builder : builder.WaitFor(dependency);
}
