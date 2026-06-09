# SonarLint connected mode

`connectedMode.json` is the **shared binding** for *SonarQube for IDE* (the plugin
formerly called SonarLint), supported by VS Code, IntelliJ / Rider, Visual Studio and
Eclipse. Committing it means every contributor's IDE auto-suggests binding this repo to
the same server + project, so the squiggles you see locally match the server's quality
profile (and "a lot of sonar issues" means the same thing for everyone).

| Key | Value |
|---|---|
| `sonarQubeUri` | `http://localhost:9000` — the **Aspire-hosted** SonarQube |
| `projectKey` | `dialysis` |

## Bring the server up

The SonarQube container is part of the dev inner loop — it starts with the AppHost:

```bash
dotnet run --project src/aspire/Dialysis.AppHost
```

Wait for `sonarqube` + `postgres-sonarqube` + `sonarqube-bootstrap` to report healthy in
the Aspire dashboard, then open <http://localhost:9000>. The bootstrap container creates
the `dialysis` project and a scanner token on first boot.

## Finish the binding in your IDE

The plugin needs a **user token** to talk to the server. Tokens are per-user and **never
committed** — enter it once in the IDE when it prompts (Connected Mode → add a SonarQube
Server connection → `http://localhost:9000`). To reuse the bootstrap token:

```bash
docker run --rm -v dialysis-sonarqube-bootstrap:/state alpine cat /state/scanner-token.txt
```

## Populate the server with a full analysis

Connected mode only mirrors what the server already knows. Run a scan to push the latest
issues + coverage (see `tools/sonarqube/scan.sh`):

```bash
tools/sonarqube/scan.sh   # reads the token from the bootstrap volume automatically
```

> Everything else the plugin writes under `.sonarlint/` (storage caches, work dir) is
> per-user state and is gitignored — only this file and `connectedMode.json` are shared.
