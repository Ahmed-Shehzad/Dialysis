using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;
using SwarmDeploy = Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm.Deploy;

namespace Dialysis.AppHost;

/// <summary>
/// Extension surface that pushes every concern that used to live in the hand-curated
/// <c>docker-compose.override.yaml</c> into the AppHost. Each method wraps
/// <c>PublishAsDockerComposeService</c> so <c>Program.cs</c> stays a flat list of
/// resource declarations.
///
/// Every mutation is a no-op when the AppHost is running under
/// <c>dotnet run</c> (the dev F5 loop): Aspire only invokes the
/// <c>PublishAsDockerComposeService</c> callbacks during the publish step.
/// </summary>
public static class ComposePublishExtensions
{
    /// <summary>Relative path from <c>deploy/compose/{env}/docker-compose.yaml</c> back to the repo root.</summary>
    private const string RepoRootFromCompose = "../../..";

    /// <summary>Container port every ASP.NET host binds when running under Docker.</summary>
    public const int AspNetContainerPort = 8080;

    /// <summary>
    /// Configures a module API for publish: <c>Dockerfile.module</c> build stanza, host
    /// port mapping, ASP.NET environment + URLs, module-specific HSTS / ForwardedHeaders /
    /// RequireAuthority / OTLP env vars, replicas.
    /// </summary>
    public static IResourceBuilder<ProjectResource> WithModuleDeployment(
        this IResourceBuilder<ProjectResource> builder,
        string projectRelativePath,
        string assemblyDllName,
        string moduleConfigPrefix,
        int hostPort,
        string environment) =>
        builder.PublishAsDockerComposeService((resource, service) =>
        {
            ApplyModuleDockerfileBuild(service, projectRelativePath, assemblyDllName);
            ApplyPublishedImageName(service, resource.Name);
            ApplyHostPort(service, hostPort, AspNetContainerPort);
            ApplyAspNetEnvironment(service, environment, AspNetContainerPort);
            ApplyModuleHardening(service, moduleConfigPrefix, environment);
            ApplyReplicas(service, environment);
        });

    /// <summary>
    /// A BFF (the legacy identity BFF or any per-context BFF) — same generic
    /// <c>Dockerfile.module</c> as the module APIs, but bound directly to its pinned host port
    /// (no 8080 indirection) because the gateway's ReverseProxy cluster targets it by hostname:port.
    /// </summary>
    public static IResourceBuilder<ProjectResource> WithBffDeployment(
        this IResourceBuilder<ProjectResource> builder,
        string projectRelativePath,
        string assemblyDllName,
        int hostPort,
        string environment) =>
        builder.PublishAsDockerComposeService((resource, service) =>
        {
            ApplyModuleDockerfileBuild(service, projectRelativePath, assemblyDllName);
            ApplyPublishedImageName(service, resource.Name);
            ApplyHostPort(service, hostPort, hostPort);
            ApplyAspNetEnvironment(service, environment, hostPort);
        });

    /// <summary>
    /// A per-context SPA — built from its own <c>Dockerfile</c> next to the sources (nginx serving
    /// the static bundle on container port 80). The gateway reaches it by service hostname on :80;
    /// the host-port map is for direct debugging. <c>BROWSER=none</c> is a Vite dev-time hint and is
    /// dropped from the published image.
    /// </summary>
    public static IResourceBuilder<NodeAppResource> WithWebDeployment(
        this IResourceBuilder<NodeAppResource> builder,
        string frontendFolder,
        int hostPort) =>
        builder.PublishAsDockerComposeService((resource, service) =>
        {
            service.Build = new Build
            {
                Context = RepoRootFromCompose + "/src/frontend/" + frontendFolder,
                Dockerfile = "Dockerfile",
            };
            ApplyPublishedImageName(service, resource.Name);
            ApplyHostPort(service, hostPort, 80);
            service.Environment.Remove("BROWSER");
        });

    /// <summary>
    /// YARP gateway — dedicated <c>Dockerfile.gateway</c>, browser-facing port, gateway HSTS
    /// + ForwardedHeaders, and every ReverseProxy cluster destination redirected from its
    /// dev-time <c>localhost</c> binding to the compose service hostname.
    /// </summary>
    public static IResourceBuilder<ProjectResource> WithGatewayDeployment(
        this IResourceBuilder<ProjectResource> builder,
        int hostPort,
        string environment,
        IReadOnlyList<(string ClusterId, string Address)> clusterOverrides) =>
        builder.PublishAsDockerComposeService((resource, service) =>
        {
            service.Build = new Build
            {
                Context = RepoRootFromCompose,
                Dockerfile = "Dockerfile.gateway",
            };
            ApplyPublishedImageName(service, resource.Name);
            ApplyHostPort(service, hostPort, hostPort);
            ApplyAspNetEnvironment(service, environment, hostPort);
            if (DeploymentEnvironment.RequiresProductionHardening(environment))
            {
                service.Environment["Gateway__UseHsts"] = "true";
                service.Environment["Gateway__UseForwardedHeaders"] = "true";
            }
            // The gateway resolves each upstream by service name inside the compose network;
            // override the dev-time localhost cluster addresses so YARP forwards to the right hop.
            foreach (var (clusterId, address) in clusterOverrides)
            {
                service.Environment[
                    $"ReverseProxy__Clusters__{clusterId}__Destinations__d1__Address"] = address;
            }
            ApplyReplicas(service, environment);
        });

    /// <summary>Per-module Postgres — host-mapped port + <c>pg_isready</c> healthcheck.</summary>
    public static IResourceBuilder<PostgresServerResource> WithPublishedDatabasePort(
        this IResourceBuilder<PostgresServerResource> builder,
        int hostPort,
        string databaseName,
        string environment) =>
        builder.PublishAsDockerComposeService((_, service) =>
        {
            ApplyHostPort(service, hostPort, 5432);
            var interval = DeploymentEnvironment.HealthcheckIntervalSeconds(environment) + "s";
            service.Healthcheck = new Healthcheck
            {
                Test = ["CMD-SHELL", $"pg_isready -U postgres -d {databaseName}"],
                Interval = interval,
                Timeout = "3s",
                Retries = 10,
                StartPeriod = "5s",
            };
        });

    /// <summary>Shared infra (RabbitMQ, Valkey, Keycloak, SonarQube) — one or more host port maps.</summary>
    public static IResourceBuilder<T> WithPublishedPorts<T>(
        this IResourceBuilder<T> builder,
        params (int Host, int Container)[] ports)
        where T : IComputeResource =>
        builder.PublishAsDockerComposeService((_, service) =>
        {
            foreach (var (host, container) in ports)
            {
                ApplyHostPort(service, host, container);
            }
        });

    // -------- private mutators --------

    private static void ApplyModuleDockerfileBuild(Service service, string project, string dll)
    {
        service.Build = new Build
        {
            Context = RepoRootFromCompose,
            Dockerfile = "Dockerfile.module",
            Args =
            {
                ["MODULE_PROJECT"] = project,
                ["MODULE_DLL"] = dll,
            },
        };
    }

    /// <summary>
    /// Names the built image <c>&lt;registry&gt;/&lt;service&gt;:&lt;tag&gt;</c> when the publish invocation
    /// carries <c>DIALYSIS_IMAGE_REGISTRY</c> (the NUKE <c>PushImages</c> flow); no-op otherwise so
    /// the committed, drift-gated compose folders keep build-only services with local names.
    /// </summary>
    private static void ApplyPublishedImageName(Service service, string serviceName)
    {
        var image = ContainerRegistryPublishExtensions.QualifiedImageName(serviceName);
        if (image is not null)
        {
            service.Image = image;
        }
    }

    private static void ApplyHostPort(Service service, int host, int container) =>
        // Service.Ports is a List<string> using compose's short syntax "host:container".
        service.Ports.Add(host + ":" + container);

    private static void ApplyAspNetEnvironment(Service service, string environment, int port)
    {
        service.Environment["ASPNETCORE_ENVIRONMENT"] =
            DeploymentEnvironment.ToAspNetCoreEnvironment(environment);
        // The dev AppHost binds to http://localhost:<port>; inside a container we need to bind
        // on all interfaces so the gateway / other services on the compose network can reach us.
        service.Environment["ASPNETCORE_URLS"] = "http://+:" + port;
    }

    private static void ApplyModuleHardening(Service service, string modulePrefix, string environment)
    {
        if (!DeploymentEnvironment.RequiresProductionHardening(environment))
        {
            return;
        }
        service.Environment[$"{modulePrefix}__UseHsts"] = "true";
        service.Environment[$"{modulePrefix}__UseForwardedHeaders"] = "true";
        service.Environment[$"{modulePrefix}__Telemetry__OtlpEndpoint"] = "http://otel-collector:4317";
        if (DeploymentEnvironment.IsProduction(environment))
        {
            // RequireAuthority is only flipped on for full prod — staging keeps it off so an
            // operator can probe the stack without a real Keycloak realm in front of it.
            service.Environment[$"{modulePrefix}__Authentication__RequireAuthorityWhenNotDevelopment"] = "true";
        }
    }

    private static void ApplyReplicas(Service service, string environment)
    {
        service.Deploy ??= new SwarmDeploy();
        service.Deploy.Replicas = DeploymentEnvironment.Replicas(environment);
    }
}
