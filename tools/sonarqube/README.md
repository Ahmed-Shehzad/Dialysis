# SonarQube dev setup

The Aspire AppHost auto-starts a SonarQube Community 2025.1 server + its
PostgreSQL on every dev launch:

```bash
dotnet run --project src/aspire/Dialysis.AppHost
```

In the Aspire dashboard you'll see three new resources start in parallel with
the module APIs:

| Resource              | Purpose                                                     |
|-----------------------|-------------------------------------------------------------|
| `postgres-sonarqube`  | Dedicated Postgres 17 backing SonarQube                     |
| `sonarqube`           | SonarQube 2025.1 community web UI (`http://localhost:9000`) |
| `sonarqube-bootstrap` | One-shot job: rotates admin pwd, creates project, mints token |

## First-boot bootstrap

The `sonarqube-bootstrap` container runs once SonarQube reports healthy and:

1. Rotates the default `admin/admin` password to `admin/DialysisDev!1` (dev-only).
2. Creates project `dialysis` ("Dialysis Modular Monolith"). Idempotent.
3. Generates a `GLOBAL_ANALYSIS_TOKEN` named `dialysis-scanner` and writes it
   to the `dialysis-sonarqube-bootstrap` Docker volume at `/state/scanner-token.txt`.

The bootstrap exits with the scan command printed in the Aspire dashboard
logs — re-runs on subsequent boots are no-ops if state is already present.

## Running a scan

```bash
# Install scanner tools once
dotnet tool install --global dotnet-sonarscanner
dotnet tool install --global dotnet-coverage

# Run the scan (pulls the token from the bootstrap volume)
tools/sonarqube/scan.sh
```

Open `http://localhost:9000/dashboard?id=dialysis` to see the report.

## Code Quality mode

SonarQube 2025.1 ships with **Multi-Quality Rule (MQR) mode** enabled by
default for new projects, which is what the project picks up automatically.
MQR enforces the four "clean code" attribute groups (Security, Reliability,
Maintainability, Coverage). No extra toggle needed; verify by opening the
project's *Administration → Quality Profiles* page after first analysis.

## GitHub integration (one-time, manual)

SonarQube can't auto-discover your GitHub App because the App's private key
is a per-user/per-org secret. The codebase wires the SonarQube side; you
provide credentials.

1. **Create a GitHub App** in your GitHub org. Permissions required:
   - **Read & Write** on Pull requests, Checks, Commit statuses
   - **Read-only** on Contents, Metadata
   - Subscribe to: `Pull request`, `Push`, `Repository`
2. Generate a private key (`.pem` file).
3. Install the App on the `Ahmed-Shehzad/Dialysis` repository.
4. In SonarQube, log in as admin, go to **Administration → DevOps Platform
   Integrations → GitHub** and click **Create configuration**:
   - **Configuration name**: `Ahmed-Shehzad`
   - **GitHub API URL**: `https://api.github.com`
   - **GitHub App ID**: from your App's settings page
   - **Client ID** + **Client secret**: from the App's OAuth section
   - **Private key**: paste the `.pem` content
   - **Webhook secret**: random string (also configure on the App side)
5. On the `dialysis` project: **Project Settings → DevOps Platform → GitHub**,
   bind to the GitHub repo `Ahmed-Shehzad/Dialysis`. PR decoration enables itself.

## Resetting

```bash
# Wipe analysis state but keep the JVM cache & extensions
docker volume rm dialysis-sonarqube-data

# Wipe the bootstrap token (forces a fresh project + token on next launch)
docker volume rm dialysis-sonarqube-bootstrap

# Nuclear — full reset including the database
docker volume rm dialysis-sonarqube-data dialysis-sonarqube-logs \
  dialysis-sonarqube-extensions dialysis-sonarqube-pg-data \
  dialysis-sonarqube-bootstrap
```
