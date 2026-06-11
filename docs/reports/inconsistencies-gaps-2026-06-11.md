# Inconsistencies & gaps — 2026-06-11

Follow-up to `improvement-areas-2026-06-10.md`. That survey's P1/P2 items (and most of P3)
landed across #187–#234. This report covers what the simulator-smoke investigation surfaced
on top: **verified inconsistencies still in the tree, structural gaps that need a design
decision, and the systemic pattern that let two startup-killing bugs hide for days.**
Every finding below was reproduced or grepped against the current tree, not inferred.

## How we got here (context)

The simulator-smoke 502 regression turned out to be three stacked bugs, all in the BFF
startup path, each masked by a different environment asymmetry:

1. **#182** wired Hangfire into every BFF → `Hangfire.PostgreSql`'s schema installer races
   itself across co-tenant hosts on one database (`XX000: could not find tuple for
   constraint`) → losing BFFs died in CI. Locally invisible (timing).
2. **9407dd4** ("style: normalize brace/line-break formatting") silently **deleted** the BFF
   and Identity-BFF Hangfire wiring — un-breaking CI by accident, while leaving the AppHost
   injecting a connection string nothing read and a `/hangfire` link that 404'd.
3. **#187**'s `KeycloakSecretGuard` then killed all seven context BFFs in CI because they
   were the only hosts without `Properties/launchSettings.json` → ran as Production under
   `--no-launch-profile` → guard threw on dev-realm secrets. Locally invisible (children
   inherit the AppHost's Development env).

Fixes merged in #234 (launch profiles) and on `claude/improvement-areas-m8vxqk` (Hangfire
restoration + advisory-lock schema install + `ValidateOnBuild`-safe scheduler DI).
simulator-smoke run 27359208505 is green on that branch.

## A. Verified inconsistencies (actionable now)

### A1. Full-stack Kubernetes charts bind the gateway and every BFF to loopback — HIGH

`deploy/charts/dialysis-{dev,staging,prod}/values.yaml` sets
`ASPNETCORE_URLS: "http://localhost:<port>"` for the **gateway (9090), Identity BFF (5275),
and all seven context BFFs (5301–5307)**. Inside a pod, a loopback bind is unreachable from
the Service — the full-stack charts' entire edge tier cannot serve traffic in a cluster. The
per-unit charts are correct (`http://+:5301`), and so is compose (`http://+:`).

Root cause: `AddContextBff` (and the gateway/identity equivalents) pick the URL shape with
`deployUnit is null ? "http://localhost:" : "http://+:"` — a full-stack publish has
`deployUnit == null`, so it gets the *run-mode* shape. The condition should branch on
`builder.ExecutionContext.IsPublishMode`. Fix is a one-line condition change per call site +
`./build.sh PublishDeployArtifacts` + commit (the drift gate enforces the regeneration).
Nobody has noticed because the full-stack charts have never been `helm install`ed for real —
the unit charts are the deployment path of record (ADR-0001).

### A2. `zaproxy/action-baseline@v0.13.0` runs on Node 20 — breaks ~2026-06-16 — HIGH

The ZAP job log now carries the GitHub warning: Node 20 actions are **forced to Node 24
starting June 16th, 2026** (five days out). Every other action is already on its Node-24
major (`checkout@v6`, `setup-dotnet@v5`, `upload-artifact@v7`, `cache@v5`, `setup-node@v6`,
`setup-python@v6`, `github-script@v8`). Bump `zaproxy/action-baseline` to its Node-24
release when available, or set `FORCE_JAVASCRIPT_ACTIONS_TO_NODE24=true` on the job now and
verify the scan still runs. Also worth a glance: `aquasecurity/trivy-action@0.35.0` and
`github/codeql-action/upload-sarif@v3` (the SARIF uploader; CodeQL itself was removed).

### A3. Vestigial `his-outbox-e2e` CI job selects zero tests — LOW

`.github/workflows/build-test.yml:131` still defines the `workflow_dispatch`-only
`his-outbox-e2e` job filtering on `HisOutboxRelayGoldenPathTests`, a class that no longer
exists — anyone dispatching it gets a green job that ran nothing. The advisory-lock relay
path it used to cover now runs in the default suite (`OutboxRelayAdvisoryLockTests`).
Decision: delete the job, or repoint the filter if the SQL-Server+RabbitMQ matrix is still
wanted.

### A4. The 9407dd4 audit is complete — the Hangfire blocks were the only casualties

A token-level diff audit of all 185 files in 9407dd4 (removed lines whose normalized form
never reappears) flags exactly three files: the two Hangfire removals (restored on this
branch) and two false positives (`SecurityHeadersMiddleware` CSP became a path-conditional
multi-line form; an Identity foreach became LINQ). No further functional reverts are hiding
in that commit.

## B. Structural gaps (need a design decision)

### B1. Co-tenant Hangfire servers share one schema and one `default` queue per database

`AddTransponderHangfire` calls `AddHangfireServer()` with default options in **every** host.
On the HIS database that is four competing workers (his-api, his-bff, identity-bff,
portal-bff) polling the same `hangfire.jobqueue` — a job enqueued by the API can execute
inside a BFF process. For `TransponderHangfirePublishJob` on a bus-less BFF that means a
runtime failure + Hangfire retry until a capable worker picks it up: eventually correct, but
nondeterministic placement and noisy retries. Options, in increasing isolation:
(a) per-host queue names (`ServerOptions.Queues = [host-slug]`, scheduler enqueues to its
own queue); (b) per-host `SchemaName`; (c) BFFs register Hangfire storage + dashboard but
**no server** (they currently have no jobs of their own — the server is pure polling cost).
Recommendation: (c) now, revisit (a) when a BFF first needs a recurring job.

### B2. No automated coverage of the two bug classes that broke smoke

Both startup killers were untestable by the existing suites because they only manifest under
a specific environment or concurrency shape:

- **Concurrent first-boot schema install**: add a Testcontainer test that runs
  `AddTransponderHangfire` from N parallel processes/threads against one fresh database
  (the manual repro that proved the race is in this branch's commit message).
- **Environment-flip boot matrix**: every host should `builder.Build()` cleanly in both
  Development (ValidateOnBuild on) *and* Production-with-real-secrets shape. A cheap
  WAF-style test per host (`WebApplicationFactory` with `UseEnvironment("Development")` and
  a stubbed Hangfire/secret config) would have caught both the `ValidateOnBuild` death and
  the secret-guard death at PR time instead of in a 30-minute smoke run.

### B3. Pinned ports are now quadruplicated with no drift gate

5275/5301–5307/9090 live in four places: the new `launchSettings.json` files, the AppHost,
the gateway's `appsettings.json` clusters, and the Keycloak realm `redirect_uri`s. All four
must agree byte-for-byte. The frontend duplicate-sync gate is the precedent; a tiny script
asserting port agreement across the four sources would close the class.

### B4. Style/refactor passes can silently revert behavior

9407dd4 deleted working code under a "style:" subject and review missed it. Process
guardrails worth adopting: (1) formatting commits must be generated by `tools/sonarqube/fix.sh`
or `dotnet format` *alone* and reviewed with `git diff -w` (whitespace-suppressed — real
deletions stand out); (2) the PR template gains a checkbox for "this change is mechanical;
`git diff -w` shows no logic edits".

## C. Carried-over operational follow-ups (unchanged from the survey)

- First real `PushImages --registry` run against the actual JFrog instance.
- First staging deployment of the unit charts (`platform` → `identity` → contexts) — which
  would also have caught A1's loopback bind in the *full-stack* charts had it used them.
- Raise the coverage gate above 70 as the suite grows (baseline was 75.0).
- Revisit the two Aspire preview workarounds when upgrading past 13.4.x.

## Branch state

`claude/improvement-areas-m8vxqk` (not yet PR'd): restores the 9407dd4-dropped Hangfire
wiring with a race-safe, ValidateOnBuild-safe implementation. Three commits
(`c79960b`, `c57b3f4`, `df680e9`); simulator-smoke green on the branch (run 27359208505).
