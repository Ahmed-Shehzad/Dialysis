namespace Dialysis.AppHost;

/// <summary>
/// Names of the deployment environments this AppHost can publish. Read from
/// configuration key <see cref="EnvVarName"/> (which the NUKE
/// <c>PublishCompose</c> target threads through as an env var on the
/// <c>dotnet run</c> invocation that drives the compose publisher).
///
/// Picking <c>prod</c> as the default keeps the legacy "regenerate the deployment
/// topology" muscle memory unchanged — operators who run
/// <c>./build.sh PublishCompose</c> without arguments still get a production
/// shape, just now under <c>deploy/compose/prod/</c>.
/// </summary>
public static class DeploymentEnvironment
{
    /// <summary>Standard dev shape — F5-equivalent topology but as a compose project.</summary>
    public const string Dev = "dev";

    /// <summary>Pre-prod shape — production hardening on, single replicas, looser healthchecks.</summary>
    public const string Staging = "staging";

    /// <summary>Full production shape — hardening on, multi-replica, tight healthchecks.</summary>
    public const string Prod = "prod";

    /// <summary>Configuration key the AppHost reads to pick the published shape.</summary>
    public const string EnvVarName = "DIALYSIS_DEPLOY_ENV";

    /// <summary>Default when nothing is provided — keep operator muscle memory pointed at prod.</summary>
    public const string Default = Prod;

    /// <summary><c>true</c> if the environment requires HSTS, RequireAuthority, and OTLP wiring.</summary>
    public static bool RequiresProductionHardening(string environment) =>
        environment is Staging or Prod;

    /// <summary><c>true</c> for the full production shape (HSTS + RequireAuthority + replicas &gt; 1).</summary>
    public static bool IsProduction(string environment) =>
        environment == Prod;

    /// <summary>ASP.NET Core environment name to stamp on every published service.</summary>
    public static string ToAspNetCoreEnvironment(string environment) => environment switch
    {
        Prod => "Production",
        Staging => "Staging",
        _ => "Development",
    };

    /// <summary>Number of replicas for horizontally-scalable services (module APIs + gateway).</summary>
    public static int Replicas(string environment) =>
        IsProduction(environment) ? 2 : 1;

    /// <summary>Healthcheck interval in seconds — relaxed on staging to keep small boxes calm.</summary>
    public static int HealthcheckIntervalSeconds(string environment) =>
        environment == Staging ? 10 : 5;
}
