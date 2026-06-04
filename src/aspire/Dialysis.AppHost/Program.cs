// Aspire orchestration for the Dialysis modular monolith.
//
// Two modes drive this AppHost:
//   * dev F5 loop — `dotnet run --project src/aspire/Dialysis.AppHost`
//     brings up Postgres / RabbitMQ / Valkey / Keycloak / SonarQube + every
//     module API + BFF + gateway + Vite SPA with hot reload.
//   * publish — `dotnet run --project src/aspire/Dialysis.AppHost --publisher compose`
//     (driven by `./build.sh PublishCompose --environment <env>`) writes a
//     self-contained `deploy/compose/<env>/docker-compose.yaml`. The same
//     resource graph is the single source of truth for dev *and* production
//     — production-only concerns (HSTS, host port mappings, the OTLP
//     collector, build stanzas, replica counts) live in
//     `ComposePublishExtensions` callbacks that only fire under
//     `builder.ExecutionContext.IsPublishMode`.

using Dialysis.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Which deployment shape are we publishing? Read once at the top so every helper sees the
// same value. Defaults to `prod` so operators who run `./build.sh PublishCompose` without
// arguments still get the production topology — `dev` and `staging` are opt-in.
var deployEnv =
    builder.Configuration[DeploymentEnvironment.EnvVarName] ?? DeploymentEnvironment.Default;
var publishing = builder.ExecutionContext.IsPublishMode;
// Aspire validates that every compute resource is bound to exactly one compute environment.
// We only register the environment for the publisher actually in use this run; both
// `--publisher compose` and `--publisher k8s` are first-class outputs but they're never
// emitted in one shot. `PublisherName` is null in dev (no environment is registered at all;
// Aspire's own dev orchestrator handles run-time).
var publisherName = builder.ExecutionContext.PublisherName;
var isComposePublish = publishing && string.Equals(publisherName, "compose", StringComparison.OrdinalIgnoreCase);
var isKubernetesPublish = publishing && string.Equals(publisherName, "k8s", StringComparison.OrdinalIgnoreCase);

// --- Deployment publisher -------------------------------------------------
// `dotnet run --project src/aspire/Dialysis.AppHost --publisher compose --output-path deploy/compose/<env>`
// renders the full topology (every Postgres, RabbitMQ, Valkey, Keycloak, SonarQube, the
// five module APIs, the BFF, the gateway, and the SPA) into a production-ready compose
// project. Every overlay concern lives in the AppHost — there is no `docker-compose.override.yaml`.
//
// WithDashboard(false): the Aspire dashboard is a dev-time tool — in deployment the
// host-side OTLP collector handles telemetry, so we omit the dashboard from the published
// compose. (Leaving it enabled also trips the publisher's "multiple compute environments"
// check on the auto-attached dashboard resource.)
//
// ConfigureComposeFile: the OTEL collector + the defensive ASPNETCORE_ENVIRONMENT stamp
// are applied here rather than per-resource because they're cross-cutting concerns.
// Aspire validates that every compute resource is bound to exactly one compute environment.
// Both `--publisher compose` and `--publisher k8s` are first-class outputs, but they never
// run in the same invocation — we register only the one matching the current publisher.
// The dev F5 loop (no publisher) registers neither; Aspire's own dev orchestrator handles
// run-time then. `k8sEnv` stays non-null only when the k8s publisher is active so the
// Ingress block at the bottom of this file can attach a route to the gateway.
Aspire.Hosting.ApplicationModel.IResourceBuilder<Aspire.Hosting.Kubernetes.KubernetesEnvironmentResource>? k8sEnv = null;
if (isComposePublish)
{
    builder.AddDockerComposeEnvironment("compose")
        .WithDashboard(false)
        .ConfigureComposeFile(file =>
        {
            // Inject the OTEL collector as an Aspire-emitted service so the override file
            // isn't needed. The collector's config lives at
            // `deploy/compose/otel-collector.yaml` (already moved out of the override during
            // #130); each compose env folder is three levels deep so the bind mount resolves
            // through ../../.
            if (!DeploymentEnvironment.RequiresProductionHardening(deployEnv))
            {
                return;
            }
            file.Services["otel-collector"] =
                new Aspire.Hosting.Docker.Resources.ComposeNodes.Service
                {
                    Name = "otel-collector",
                    Image = "otel/opentelemetry-collector-contrib:0.110.0",
                    Command = { "--config=/etc/otel-collector.yaml" },
                    Volumes =
                    {
                        new Aspire.Hosting.Docker.Resources.ServiceNodes.Volume
                        {
                            Name = "otel-collector-config",
                            Type = "bind",
                            Source = "../otel-collector.yaml",
                            Target = "/etc/otel-collector.yaml",
                            ReadOnly = true,
                        },
                    },
                    Ports =
                    {
                        "4317:4317",
                        "4318:4318",
                    },
                };
        });
}

// --- Kubernetes / Helm publisher -----------------------------------------
// `dotnet run --project src/aspire/Dialysis.AppHost --publisher k8s --output-path
//  deploy/charts/dialysis-<env>` writes a complete Helm chart for the topology.
// Operators install with:
//   helm install dialysis deploy/charts/dialysis-prod -n dialysis --create-namespace
//
// The chart name + release name + namespace embed the deployment environment so
// dev / staging / prod can co-exist on one cluster without collisions. Aspire's
// k8s publisher renders one Deployment per project + one Service per Endpoint +
// ConfigMap/Secret for env vars; an Ingress declared at the bottom of this file
// covers the browser-facing surface (Gateway → cluster external).
if (isKubernetesPublish)
{
    k8sEnv = builder.AddKubernetesEnvironment("k8s")
        .WithDashboard(false)
        .WithHelm(helm =>
        {
            helm.WithChartName("dialysis-" + deployEnv);
            helm.WithChartVersion("0.1.0");
            helm.WithChartDescription(
                $"Dialysis modular monolith ({deployEnv}) — every module API, the BFF, the gateway, " +
                "the SPA, plus per-module Postgres, RabbitMQ, Valkey, Keycloak. Generated from the " +
                "Aspire AppHost; do not hand-edit the manifests, re-run the NUKE PublishKubernetes target.");
            helm.WithNamespace("dialysis-" + deployEnv);
            helm.WithReleaseName("dialysis");
        });
}

// --- Constants -------------------------------------------------------------
// Centralized so the Keycloak port, realm, and import-volume path can't drift
// between the container definition and the consumers that build URIs from them.
const string keycloakRealm = "dialysis";
const int keycloakHostPort = 8081;
const int keycloakContainerPort = 8080;
// Bind mount is relative to this csproj (src/aspire/Dialysis.AppHost). The canonical
// realm export lives with the Identity module so it ships next to its own docker-compose;
// the repo-root `keycloak/` directory is intentionally empty.
const string keycloakRealmImportPath = "../../backend/Identity/keycloak";
const string keycloakDiscoveryPath = "/realms/" + keycloakRealm + "/.well-known/openid-configuration";

// BFF + Gateway ports must be pinned (not random) so the redirect_uri the OIDC handler
// builds matches what's registered on the dialysis-bff Keycloak client in dialysis-realm.json
// (redirectUris: http://localhost:5275/* and http://localhost:9090/*).
const int identityBffPort = 5275;
const int gatewayPort = 9090;

// --- Shared infrastructure -------------------------------------------------

var rabbit = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

var valkey = builder.AddValkey("valkey")
    .WithLifetime(ContainerLifetime.Persistent);

// Aspire.Hosting.Keycloak has no stable 13.2.x release matching the rest of the bundle, so
// we provision Keycloak via a generic container resource. The realm import volume + dev-friendly
// args mirror the existing docker-compose Keycloak service.
//
// WithHttpHealthCheck against the realm's OIDC discovery endpoint is what makes downstream
// `.WaitFor(keycloak)` actually block until the realm is imported and serving — otherwise
// WaitFor returns the instant the container reports Running (process started), and consumers
// like the BFF race ahead and hit IDX20803 (unable to obtain OIDC configuration).
var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.0")
    .WithArgs("start-dev", "--import-realm")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
    .WithEnvironment("KC_DB", "dev-mem")
    .WithEnvironment("KC_HEALTH_ENABLED", "true")
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithHttpEndpoint(port: keycloakHostPort, targetPort: keycloakContainerPort, name: "http")
    .WithHttpHealthCheck(keycloakDiscoveryPath, statusCode: 200, endpointName: "http");
// Realm-import bind mount is only meaningful for run-time + compose: the k8s publisher
// rejects bind mounts (Aspire 13.4) and a production k8s deployment of Keycloak would
// import the realm via the operator's own ConfigMap or a sidecar init container instead.
// For k8s the chart ships Keycloak without the realm; operators wire `Authentication__Authority`
// per-cluster via Helm values.yaml.
if (!isKubernetesPublish)
{
    keycloak.WithBindMount(keycloakRealmImportPath, "/opt/keycloak/data/import", isReadOnly: true);
}
// Deliberately NOT Persistent: --import-realm only imports if the realm doesn't
// already exist, so a long-lived container makes dialysis-realm.json edits invisible
// (redirect_uri / client / role changes silently ignored). With KC_DB=dev-mem the
// in-container state is ephemeral by design, so re-creating the container each run
// is cheap and keeps the realm in lockstep with the file.

var keycloakRealmUri = ReferenceExpression.Create(
    $"{keycloak.GetEndpoint("http")}/realms/{keycloakRealm}");

// --- Per-module Postgres ---------------------------------------------------

static IResourceBuilder<PostgresServerResource> Pg(IDistributedApplicationBuilder b, string name) =>
    b.AddPostgres(name)
        .WithImage("postgres", "17-alpine")
        .WithDataVolume($"dialysis-{name}-data")
        .WithLifetime(ContainerLifetime.Persistent);

// PDMS gets the TimescaleDB image — same Postgres wire protocol + drivers, plus the
// `timescaledb` extension that the IntradialyticReadings table needs (hypertable +
// compression policy declared by the EF migration). Other modules stay on plain
// postgres-alpine because they don't have the high-volume time-series shape.
static IResourceBuilder<PostgresServerResource> PgTimescale(IDistributedApplicationBuilder b, string name) =>
    b.AddPostgres(name)
        .WithImage("timescale/timescaledb", "latest-pg17")
        .WithDataVolume($"dialysis-{name}-data")
        .WithLifetime(ContainerLifetime.Persistent);

// Keep the server builders so the publish-time decoration block at the bottom can map the
// host-side port + healthcheck onto each one. The DB-wrapper builder (.AddDatabase) is what
// downstream resources use for the connection string, but the host port mapping belongs on
// the server resource.
var hisPgServer = Pg(builder, "postgres-his");
var ehrPgServer = Pg(builder, "postgres-ehr");
var pdmsPgServer = PgTimescale(builder, "postgres-pdms");
var smartconnectPgServer = Pg(builder, "postgres-smartconnect");
var hiePgServer = Pg(builder, "postgres-hie");

var hisDb = hisPgServer.AddDatabase("His", databaseName: "dialysis_his");
var ehrDb = ehrPgServer.AddDatabase("Ehr", databaseName: "dialysis_ehr");
var pdmsDb = pdmsPgServer.AddDatabase("Pdms", databaseName: "dialysis_pdms");
var smartconnectDb = smartconnectPgServer.AddDatabase("SmartConnect", databaseName: "dialysis_smartconnect");
var hieDb = hiePgServer.AddDatabase("Hie", databaseName: "dialysis_hie");

// --- SonarQube (auto-start with the AppHost) ------------------------------
//
// SonarQube Community 2025.1 static-analysis server. Starts in parallel with the
// module Postgres + RabbitMQ on every AppHost launch so devs can hit
// http://localhost:9000 without a separate startup ritual.
//
// Operational caveats devs should know:
//   • The image is ~700 MB on first pull and needs ~2 GB RAM at idle. Container
//     lifetime is Persistent so re-launches reuse the warm cache.
//   • Embedded Elasticsearch requires `sysctl -w vm.max_map_count=524288` on the
//     host. Docker Desktop sets this automatically; bare-metal Linux often
//     doesn't, and SonarQube exits with a clear error if the limit is too low.
//   • First-boot bootstrap (admin/admin → "dialysis" project + analysis token)
//     is automated by the sonarqube-bootstrap container; it writes the token to
//     a named volume and logs the scan command.
//   • Scanner runs are NOT triggered from the AppHost — analysing the full
//     solution takes 5-10 minutes and shouldn't block dev iteration. Use
//     `tools/sonarqube/scan.sh` to run the scanner against the live server.
//   • GitHub integration (DevOps Platform) needs a GitHub App's credentials.
//     See tools/sonarqube/README.md for the click-through setup.
//
// Sonar's Postgres is dedicated — the analyzer's lifecycle is unrelated to the
// module DBs, and mixing them would let one set of restarts blow away the other.
// The JDBC password is pinned via an Aspire parameter (default "sonar") so devs
// can override per-machine via user-secrets without rewriting the AppHost.
//
// SonarQube reaches its Postgres via the DCP-managed container network using the
// resource name as the hostname; inside that network, Postgres listens on its
// container target port (5432), not the random host-allocated port.
var sonarPgPwd = builder.AddParameter("sonar-pg-password", "sonar", secret: true);

var sonarPgServer = builder.AddPostgres("postgres-sonarqube", password: sonarPgPwd)
    .WithImage("postgres", "17-alpine")
    .WithDataVolume("dialysis-sonarqube-pg-data")
    .WithLifetime(ContainerLifetime.Persistent);

// Pinned to the latest 26.x community image. Docker Hub tags follow YY.M.0.BUILD
// (e.g. 26.5.0.122743-community), not the marketing "2025.x" naming in the doc
// URLs — the prior `2025.1-community` attempt 404'd at pull time. Bumping the
// digest is a deliberate operator action since SonarQube reindexes on major
// upgrades and we don't want a transparent latest-tag pull to surprise devs.
var sonarqube = builder.AddContainer("sonarqube", "sonarqube", "26.5.0.122743-community")
    .WithEnvironment("SONAR_JDBC_URL", "jdbc:postgresql://postgres-sonarqube:5432/postgres")
    .WithEnvironment("SONAR_JDBC_USERNAME", "postgres")
    .WithEnvironment("SONAR_JDBC_PASSWORD", sonarPgPwd)
    // Three named volumes so a `docker volume rm` to reset analysis state doesn't
    // wipe the JVM caches or extensions (plugins) along with the index. SonarQube's
    // own runbooks treat these as three independent backups too.
    .WithVolume("dialysis-sonarqube-data", "/opt/sonarqube/data")
    .WithVolume("dialysis-sonarqube-logs", "/opt/sonarqube/logs")
    .WithVolume("dialysis-sonarqube-extensions", "/opt/sonarqube/extensions")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "http")
    // /api/system/status returns {"status":"UP"} when ready; SonarQube's startup is
    // multi-phase (web → compute engine → search) and the port opens well before
    // analysis is accepted, so a port-probe isn't enough.
    .WithHttpHealthCheck("/api/system/status", statusCode: 200, endpointName: "http")
    .WithLifetime(ContainerLifetime.Persistent)
    .WaitFor(sonarPgServer);

// One-shot bootstrap container: waits for SonarQube health, creates the "dialysis"
// project (idempotent), generates an analysis token, and writes both to a named
// volume so `tools/sonarqube/scan.sh` can read them without prompting. Uses the
// SonarQube REST API with admin/admin (the fresh-install default) — first run
// changes the password to a known dev value so the bootstrap stays idempotent.
//
// Code Quality / MQR mode: SonarQube 2025.1 defaults new projects to
// Multi-Quality Rule (MQR) mode, which is what the user asked for as "code
// quality mode on". No explicit toggle needed; the bootstrap script just creates
// the project and the default profile applies.
//
// SonarQube is a dev-time analyzer, not a deployment concern — exclude it from
// the k8s chart entirely (operators run Sonar on their own infrastructure).
// The bind-mounted bootstrap script also can't ship to k8s (publisher rejects
// bind mounts); skipping the registration for k8s avoids the whole branch.
if (!isKubernetesPublish)
{
    builder.AddContainer("sonarqube-bootstrap", "curlimages/curl", "8.11.0")
    .WithBindMount("../../../tools/sonarqube/bootstrap.sh", "/bootstrap.sh", isReadOnly: true)
    .WithVolume("dialysis-sonarqube-bootstrap", "/state")
    .WithContainerRuntimeArgs("--user", "0:0")
    .WithEnvironment("SONAR_URL", "http://sonarqube:9000")
    .WithEnvironment("SONAR_ADMIN_USER", "admin")
    // Dev-only credential; rotated by the bootstrap script on first run, then
    // re-used across launches. Real deployments must NOT reuse this.
    .WithEnvironment("SONAR_ADMIN_PASSWORD", "admin")
    .WithEnvironment("SONAR_PROJECT_KEY", "dialysis")
    .WithEnvironment("SONAR_PROJECT_NAME", "Dialysis Modular Monolith")
    .WithEntrypoint("/bin/sh")
    .WithArgs("/bootstrap.sh")
    .WithLifetime(ContainerLifetime.Session)
    .WaitFor(sonarqube);
}

// --- Module hosts ----------------------------------------------------------
//
// Each module reads infra coordinates from module-scoped config keys
// (e.g. His:Transponder:RabbitMq:ConnectionUri). We bind those keys to the
// Aspire-managed resources via WithEnvironment so a single F5 fully wires
// everything without per-module appsettings overrides.

var hisApi = builder.AddProject<Projects.Dialysis_HIS_Api>("his-api")
    .WithReference(hisDb).WaitFor(hisDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithReference(valkey).WaitFor(valkey)
    .WaitFor(keycloak)
    .WithEnvironment("His__Transponder__EnableOutboxRelay", "true")
    .WithEnvironment("His__Transponder__RabbitMq__ConnectionUri", rabbit)
    .WithEnvironment("His__DistributedCache__Valkey__ConnectionString", valkey)
    .WithEnvironment("His__Authentication__Authority", keycloakRealmUri)
    .WithEnvironment("His__Authentication__Audience", "account")
    .WithEnvironment("His__Fhir__Enabled", "true");

var ehrApi = builder.AddProject<Projects.Dialysis_EHR_Api>("ehr-api")
    .WithReference(ehrDb).WaitFor(ehrDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithReference(valkey).WaitFor(valkey)
    .WaitFor(keycloak)
    .WithEnvironment("Ehr__Transponder__EnableOutboxRelay", "true")
    .WithEnvironment("Ehr__Transponder__RabbitMq__ConnectionUri", rabbit)
    .WithEnvironment("Ehr__DistributedCache__Valkey__ConnectionString", valkey)
    .WithEnvironment("Ehr__Authentication__Authority", keycloakRealmUri)
    .WithEnvironment("Ehr__Authentication__Audience", "account")
    .WithEnvironment("Ehr__Demo__Enabled", "true")
    .WithEnvironment("Ehr__Demo__RegistrationSimulator", "true");

var pdmsApi = builder.AddProject<Projects.Dialysis_PDMS_Api>("pdms-api")
    .WithReference(pdmsDb).WaitFor(pdmsDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithReference(valkey).WaitFor(valkey)
    .WaitFor(keycloak)
    .WithEnvironment("Pdms__Transponder__EnableOutboxRelay", "true")
    .WithEnvironment("Pdms__Transponder__RabbitMq__ConnectionUri", rabbit)
    .WithEnvironment("Pdms__DistributedCache__Valkey__ConnectionString", valkey)
    .WithEnvironment("Pdms__Authentication__Authority", keycloakRealmUri)
    .WithEnvironment("Pdms__Authentication__Audience", "account")
    .WithEnvironment("Pdms__Demo__Enabled", "true")
    .WithEnvironment("Pdms__Demo__VitalsTicker", "true")
    .WithEnvironment("Pdms__Demo__MachineTelemetrySimulator", "true");

var smartConnectApi = builder.AddProject<Projects.Dialysis_SmartConnect_Api>("smartconnect-api")
    .WithReference(smartconnectDb).WaitFor(smartconnectDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithReference(valkey).WaitFor(valkey)
    .WaitFor(keycloak)
    .WithEnvironment("SmartConnect__Transponder__EnableOutboxRelay", "true")
    .WithEnvironment("SmartConnect__Transponder__RabbitMq__ConnectionUri", rabbit)
    .WithEnvironment("SmartConnect__DistributedCache__Valkey__ConnectionString", valkey)
    .WithEnvironment("SmartConnect__Authentication__Authority", keycloakRealmUri)
    .WithEnvironment("SmartConnect__Authentication__Audience", "account")
    .WithEnvironment("SmartConnect__Demo__Enabled", "true")
    .WithEnvironment("SmartConnect__Demo__Hl7Simulator", "true")
    // Bi-directional routing demo: declare two source-connector instances so a dev sees
    // SourceConnectorHostedService start an MLLP listener + a file-drop watcher concurrently,
    // both dispatching into the same demo flow. The always-on HTTP source provides the third
    // simultaneous inbound. database-reader is intentionally not wired (no demo target DB).
    .WithEnvironment("SmartConnect__SourceConnectors__0__Name", "Hl7Mllp")
    .WithEnvironment("SmartConnect__SourceConnectors__0__Kind", "mllp")
    .WithEnvironment("SmartConnect__SourceConnectors__0__DefaultFlowId", "b1d10001-0001-4000-8000-000000000001")
    .WithEnvironment("SmartConnect__SourceConnectors__0__Parameters__Port", "2575")
    .WithEnvironment("SmartConnect__SourceConnectors__1__Name", "LocalDrop")
    .WithEnvironment("SmartConnect__SourceConnectors__1__Kind", "file-reader")
    .WithEnvironment("SmartConnect__SourceConnectors__1__DefaultFlowId", "b1d10001-0001-4000-8000-000000000001")
    .WithEnvironment("SmartConnect__SourceConnectors__1__Parameters__Directory", "./tmp/smartconnect/drop")
    .WithEnvironment("SmartConnect__SourceConnectors__1__Parameters__Pattern", "*.hl7");

var hieApi = builder.AddProject<Projects.Dialysis_HIE_Api>("hie-api")
    .WithReference(hieDb).WaitFor(hieDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithReference(valkey).WaitFor(valkey)
    .WaitFor(keycloak)
    .WithEnvironment("Hie__Transponder__RabbitMq__ConnectionUri", rabbit)
    .WithEnvironment("Hie__DistributedCache__Valkey__ConnectionString", valkey)
    .WithEnvironment("Hie__Authentication__Authority", keycloakRealmUri)
    .WithEnvironment("Hie__Authentication__Audience", "account")
    .WithEnvironment("Hie__Demo__Enabled", "true");

var identityBff = builder.AddProject<Projects.Dialysis_Identity_Bff>("identity-bff")
    // Pin the BFF to a deterministic host port. The dialysis-bff Keycloak client only
    // accepts redirect_uris under http://localhost:5275/* (and the gateway port). Letting
    // Aspire allocate randomly per session breaks the OIDC handshake. Mutate the existing
    // "http" endpoint that Aspire created from the project's launchSettings rather than
    // adding a new one (which collides on the endpoint name). ASPNETCORE_URLS is also
    // forced via env so the project binds the right port even if WithEndpoint metadata
    // doesn't fully override launchSettings.
    .WithEndpoint("http", e =>
    {
        e.Port = identityBffPort;
        e.TargetPort = identityBffPort;
        e.IsProxied = false;
    })
    .WithEnvironment("ASPNETCORE_URLS",
        "http://localhost:" + identityBffPort.ToString(System.Globalization.CultureInfo.InvariantCulture))
    .WaitFor(keycloak)
    .WaitFor(hisApi)
    // BFF binds Keycloak under section "Identity:Keycloak" (KeycloakBffOptions.SectionName).
    // appsettings.Development.json hardcodes Authority to localhost:8080 — env vars from
    // Aspire override it via the IConfiguration provider chain (env > json).
    .WithEnvironment("Identity__Keycloak__Authority", keycloakRealmUri)
    // BFF's YARP cluster "his" defaults to localhost:5288 in appsettings; redirect it to
    // the Aspire-allocated HIS endpoint so token-exchange + proxied API calls resolve.
    .WithEnvironment("ReverseProxy__Clusters__his__Destinations__d1__Address", hisApi.GetEndpoint("http"));

var gateway = builder.AddProject<Projects.Dialysis_Module_Gateway>("gateway")
    // Pin the gateway port too — it is the single browser-facing origin in dev:
    //   /identity/* → BFF, /api/*, /fhir/*, /hubs/* → module APIs, /{**catch-all} → Vite SPA.
    // Keeping all requests on the gateway origin avoids cross-origin cookie loss after the
    // OIDC redirect (Keycloak sends the browser to the redirect_uri we registered, which
    // must be on the same origin that hosts the SPA cookies).
    .WithEndpoint("http", e =>
    {
        e.Port = gatewayPort;
        e.TargetPort = gatewayPort;
        e.IsProxied = false;
    })
    .WithEnvironment("ASPNETCORE_URLS",
        "http://localhost:" + gatewayPort.ToString(System.Globalization.CultureInfo.InvariantCulture))
    .WithReference(hisApi).WaitFor(hisApi)
    .WithReference(ehrApi).WaitFor(ehrApi)
    .WithReference(pdmsApi).WaitFor(pdmsApi)
    .WithReference(smartConnectApi).WaitFor(smartConnectApi)
    .WithReference(hieApi).WaitFor(hieApi)
    .WithReference(identityBff).WaitFor(identityBff)
    .WithEnvironment("Gateway__Authority", keycloakRealmUri)
    .WithEnvironment("Gateway__Audience", "account")
    .WithEnvironment("Gateway__RequireAuthentication", "true")
    // Pin the YARP identity cluster destination to the pinned BFF port (default in appsettings
    // already matches, but make it explicit so a future port change in one place doesn't desync).
    .WithEnvironment("ReverseProxy__Clusters__identity__Destinations__d1__Address",
        "http://localhost:" + identityBffPort.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/");

// The gateway is the browser-facing surface — only resource that ever crosses the cluster
// boundary. `WithExternalHttpEndpoints` lets the k8s publisher route the Ingress to it; the
// dev loop and the compose publisher treat it as a hint (already exposed via the pinned
// host port). Module APIs / BFF / SPA stay internal and reach the browser only through the
// gateway, so we deliberately don't mark them external.
if (isKubernetesPublish)
{
    gateway.WithExternalHttpEndpoints();
}

// --- Frontend (Vite dev server) -------------------------------------------
//
// Browser entry point is the gateway at http://localhost:9090 — the gateway proxies
// /{**catch-all} to Vite via the new "web" YARP cluster. The SPA itself is therefore an
// internal-only resource: WithExternalHttpEndpoints is intentionally NOT called here so
// users don't land on the SPA's own port (which would be cross-origin to the gateway and
// break the OIDC cookie round trip).
//
// VITE_GATEWAY_URL / VITE_API_BASE_URL are kept for two reasons: (a) the Vite dev proxy
// still resolves API calls when the SPA is opened directly during local debugging, and
// (b) apiClient.ts uses VITE_API_BASE_URL as axios baseURL when set. Both point at the
// gateway so that all API/identity calls stay on the gateway origin.
//
// Dependency install: `npm run dev` runs the `predev` script (defined in package.json),
// which executes `npm install`. That makes `node_modules` self-healing on every AppHost
// start — no manual `npm install` step required, and a no-op after the first install when
// package-lock.json is already in sync.
const int vitePort = 5173;
var web = builder.AddNpmApp("web", "../../frontend/dialysis-web", "dev")
    .WithReference(gateway).WaitFor(gateway)
    .WithEnvironment("BROWSER", "none")
    .WithEnvironment("VITE_GATEWAY_URL", gateway.GetEndpoint("http"))
    .WithEnvironment("VITE_API_BASE_URL", gateway.GetEndpoint("http"))
    // Pin the Vite port so the gateway's "web" YARP cluster (Address: http://localhost:5173/)
    // can reliably reach it. isProxied:false skips DCP proxy entirely — there is no need
    // for it now that the gateway is the single browser-facing origin.
    .WithHttpEndpoint(env: "PORT", port: vitePort, targetPort: vitePort, isProxied: false)
    .PublishAsDockerFile();

// --- Compose-publish decoration -------------------------------------------
// Every overlay concern that used to live in `docker-compose.override.yaml` is applied
// here as a `PublishAsDockerComposeService` callback — runs only under the compose
// publisher. The dev F5 loop and the k8s publisher both bypass this block; the k8s
// publisher's per-resource decoration is its own block further down.
if (isComposePublish)
{
    hisApi.WithModuleDeployment(
        projectRelativePath: "src/backend/HIS/Dialysis.HIS.Api/Dialysis.HIS.Api.csproj",
        assemblyDllName: "Dialysis.HIS.Api.dll",
        moduleConfigPrefix: "His",
        hostPort: 5288,
        environment: deployEnv);
    ehrApi.WithModuleDeployment(
        projectRelativePath: "src/backend/EHR/Dialysis.EHR.Api/Dialysis.EHR.Api.csproj",
        assemblyDllName: "Dialysis.EHR.Api.dll",
        moduleConfigPrefix: "Ehr",
        hostPort: 5289,
        environment: deployEnv);
    pdmsApi.WithModuleDeployment(
        projectRelativePath: "src/backend/PDMS/Dialysis.PDMS.Api/Dialysis.PDMS.Api.csproj",
        assemblyDllName: "Dialysis.PDMS.Api.dll",
        moduleConfigPrefix: "Pdms",
        hostPort: 5290,
        environment: deployEnv);
    smartConnectApi.WithModuleDeployment(
        projectRelativePath: "src/backend/SmartConnect/Api/Dialysis.SmartConnect.Api/Dialysis.SmartConnect.Api.csproj",
        assemblyDllName: "Dialysis.SmartConnect.Api.dll",
        moduleConfigPrefix: "SmartConnect",
        hostPort: 5291,
        environment: deployEnv);
    hieApi.WithModuleDeployment(
        projectRelativePath: "src/backend/HIE/Dialysis.HIE.Api/Dialysis.HIE.Api.csproj",
        assemblyDllName: "Dialysis.HIE.Api.dll",
        moduleConfigPrefix: "Hie",
        hostPort: 5292,
        environment: deployEnv);
    identityBff.WithIdentityBffDeployment(hostPort: identityBffPort, environment: deployEnv);
    gateway.WithGatewayDeployment(hostPort: gatewayPort, identityBffHostPort: identityBffPort, environment: deployEnv);

    hisPgServer.WithPublishedDatabasePort(hostPort: 5440, databaseName: "dialysis_his", environment: deployEnv);
    smartconnectPgServer.WithPublishedDatabasePort(hostPort: 5441, databaseName: "dialysis_smartconnect", environment: deployEnv);
    ehrPgServer.WithPublishedDatabasePort(hostPort: 5442, databaseName: "dialysis_ehr", environment: deployEnv);
    pdmsPgServer.WithPublishedDatabasePort(hostPort: 5443, databaseName: "dialysis_pdms", environment: deployEnv);
    hiePgServer.WithPublishedDatabasePort(hostPort: 5445, databaseName: "dialysis_hie", environment: deployEnv);

    rabbit.WithPublishedPorts((5672, 5672), (15672, 15672));
    valkey.WithPublishedPorts((6379, 6379));
    keycloak.WithPublishedPorts((keycloakHostPort, keycloakContainerPort));
    sonarqube.WithPublishedPorts((9000, 9000));

    web.WithWebDeployment(hostPort: 8080);
}

// --- Kubernetes Ingress ---------------------------------------------------
// Browser-facing surface for the k8s publisher. Routes the cluster's external traffic to
// the gateway service (port 9090 inside the cluster); the gateway then fans out to
// /identity/* (BFF), /api/* /fhir/* /hubs/* (module APIs), and /{**catch-all} (SPA) as it
// does in dev. Operators override the hostname + TLS secret in the published Helm chart's
// values.yaml per-cluster.
//
// NGINX is the default ingress class — most portable; operators on EKS swap for `alb`, on
// AKS for `azure-application-gateway`, on bare-metal can use Traefik. Override via
// `helm install --set ingress.className=...`.
if (isKubernetesPublish && k8sEnv is not null)
{
    k8sEnv.AddIngress("dialysis")
        .WithIngressClass("nginx")
        .WithHostname("dialysis." + deployEnv + ".local")
        .WithPath("/", gateway.GetEndpoint("http"), Aspire.Hosting.Kubernetes.IngressPathType.Prefix);
}

await builder.Build().RunAsync().ConfigureAwait(false);
