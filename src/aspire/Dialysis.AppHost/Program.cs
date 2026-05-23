// Aspire orchestration for the Dialysis modular monolith.
//
// Mirrors the containerized deployment stack (docker-compose.modules.yml):
//   • per-module Postgres (HIS, EHR, PDMS, SmartConnect, HIE)
//   • shared RabbitMQ, Valkey, Keycloak (realm auto-imported)
//   • each module API + Identity BFF + edge Gateway wired to the right infra
//
// Run with `dotnet run --project src/aspire/Dialysis.AppHost`. The Aspire dashboard
// (logs / metrics / traces) opens automatically; OTLP endpoint is injected into every
// project via OTEL_EXPORTER_OTLP_ENDPOINT (ModuleTelemetryExtensions falls back to it
// when <Module>:Telemetry:OtlpEndpoint is unset).

var builder = DistributedApplication.CreateBuilder(args);

// --- Constants -------------------------------------------------------------
// Centralized so the Keycloak port, realm, and import-volume path can't drift
// between the container definition and the consumers that build URIs from them.
const string KeycloakRealm = "dialysis";
const int KeycloakHostPort = 8081;
const int KeycloakContainerPort = 8080;
// Bind mount is relative to this csproj (src/aspire/Dialysis.AppHost). The canonical
// realm export lives with the Identity module so it ships next to its own docker-compose;
// the repo-root `keycloak/` directory is intentionally empty.
const string KeycloakRealmImportPath = "../../backend/Identity/keycloak";
const string KeycloakDiscoveryPath = "/realms/" + KeycloakRealm + "/.well-known/openid-configuration";

// BFF + Gateway ports must be pinned (not random) so the redirect_uri the OIDC handler
// builds matches what's registered on the dialysis-bff Keycloak client in dialysis-realm.json
// (redirectUris: http://localhost:5275/* and http://localhost:9090/*).
const int IdentityBffPort = 5275;
const int GatewayPort = 9090;

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
    .WithBindMount(KeycloakRealmImportPath, "/opt/keycloak/data/import", isReadOnly: true)
    .WithHttpEndpoint(port: KeycloakHostPort, targetPort: KeycloakContainerPort, name: "http")
    .WithHttpHealthCheck(KeycloakDiscoveryPath, statusCode: 200, endpointName: "http");
    // Deliberately NOT Persistent: --import-realm only imports if the realm doesn't
    // already exist, so a long-lived container makes dialysis-realm.json edits invisible
    // (redirect_uri / client / role changes silently ignored). With KC_DB=dev-mem the
    // in-container state is ephemeral by design, so re-creating the container each run
    // is cheap and keeps the realm in lockstep with the file.

var keycloakRealmUri = ReferenceExpression.Create(
    $"{keycloak.GetEndpoint("http")}/realms/{KeycloakRealm}");

// --- Per-module Postgres ---------------------------------------------------

static IResourceBuilder<PostgresServerResource> Pg(IDistributedApplicationBuilder b, string name) =>
    b.AddPostgres(name)
        .WithImage("postgres", "17-alpine")
        .WithDataVolume($"dialysis-{name}-data")
        .WithLifetime(ContainerLifetime.Persistent);

var hisDb         = Pg(builder, "postgres-his").AddDatabase("His", databaseName: "dialysis_his");
var ehrDb         = Pg(builder, "postgres-ehr").AddDatabase("Ehr", databaseName: "dialysis_ehr");
var pdmsDb        = Pg(builder, "postgres-pdms").AddDatabase("Pdms", databaseName: "dialysis_pdms");
var smartconnectDb = Pg(builder, "postgres-smartconnect").AddDatabase("SmartConnect", databaseName: "dialysis_smartconnect");
var hieDb         = Pg(builder, "postgres-hie").AddDatabase("Hie", databaseName: "dialysis_hie");

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

// Pinned to 2025.1 community per the user's referenced release notes; bumping the
// digest is a deliberate operator action since SonarQube reindexes on major
// upgrades and we don't want a transparent latest-tag pull to surprise devs.
var sonarqube = builder.AddContainer("sonarqube", "sonarqube", "2025.1-community")
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
builder.AddContainer("sonarqube-bootstrap", "curlimages/curl", "8.11.0")
    .WithBindMount("../../../tools/sonarqube/bootstrap.sh", "/bootstrap.sh", isReadOnly: true)
    .WithVolume("dialysis-sonarqube-bootstrap", "/state")
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
    .WithEnvironment("SmartConnect__Demo__Hl7Simulator", "true");

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
        e.Port = IdentityBffPort;
        e.TargetPort = IdentityBffPort;
        e.IsProxied = false;
    })
    .WithEnvironment("ASPNETCORE_URLS",
        "http://localhost:" + IdentityBffPort.ToString(System.Globalization.CultureInfo.InvariantCulture))
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
        e.Port = GatewayPort;
        e.TargetPort = GatewayPort;
        e.IsProxied = false;
    })
    .WithEnvironment("ASPNETCORE_URLS",
        "http://localhost:" + GatewayPort.ToString(System.Globalization.CultureInfo.InvariantCulture))
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
        "http://localhost:" + IdentityBffPort.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/");

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
const int VitePort = 5173;
builder.AddNpmApp("web", "../../frontend/dialysis-web", "dev")
    .WithReference(gateway).WaitFor(gateway)
    .WithEnvironment("BROWSER", "none")
    .WithEnvironment("VITE_GATEWAY_URL", gateway.GetEndpoint("http"))
    .WithEnvironment("VITE_API_BASE_URL", gateway.GetEndpoint("http"))
    // Pin the Vite port so the gateway's "web" YARP cluster (Address: http://localhost:5173/)
    // can reliably reach it. isProxied:false skips DCP proxy entirely — there is no need
    // for it now that the gateway is the single browser-facing origin.
    .WithHttpEndpoint(env: "PORT", port: VitePort, targetPort: VitePort, isProxied: false)
    .PublishAsDockerFile();

await builder.Build().RunAsync().ConfigureAwait(false);
