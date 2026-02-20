---
name: Longer-term Docker, Intercessor & Docs
overview: Fix assembly scan in Docker, align Intercessor registration, and update documentation for Seeder and migrations.
todos:
  - id: investigate-assembly-scan
    content: "Investigate root cause of Scrutor assembly scan failure in Docker"
    status: completed
  - id: fix-intercessor-docker
    content: "Revisit Intercessor registration once scan works (remove explicit fallbacks)"
    status: pending
  - id: update-docs-seeder
    content: "Update docs with Seeder usage and targeted rebuild tip"
    status: completed
  - id: update-docs-migrations
    content: "Update docs with migration flow (prune & InitialCreate)"
    status: completed
isProject: false
---

# Longer-term: Docker Assembly Scan, Intercessor & Docs

## Context

- **Assembly scan**: In Docker, Scrutor's `FromAssemblies` did not discover `IngestOruBatchCommandHandler` (Treatment) and `IngestOruR40MessageCommandHandler` (Alarm). Explicit fallback registrations were added.
- **Intercessor**: Per `.cursor/rules/intercessor.mdc`, explicit handler registration is allowed when scan fails in containers. Goal: assembly-only once scan works.
- **Docs**: Seeder usage, targeted rebuild for failures, and migration flow (prune + InitialCreate) need documentation.

---

## 1. Assembly Scan Root Cause (Investigation)

### Possible Causes

| Cause | Mitigation |
|-------|------------|
| IL trimming | `-p:PublishTrimmed=false` already in Dockerfile |
| Assembly load order | `typeof(X).Assembly` forces load; ensure Application assembly is in publish output |
| Scrutor `GetTypes()` edge case | Add diagnostic logging to count discovered types |
| .NET 10 / SDK mismatch | Align build and runtime images (both 10.0) |

### Investigation Steps

1. **Add diagnostic logging** in `IntercessorBuilder.Build()` before Scrutor scan:
   - Log assembly names and `assembly.GetTypes().Length` for each assembly
   - Run locally and in Docker; compare counts

2. **Verify publish output**: In Docker build, list `/app/*.dll` and confirm `Dialysis.Treatment.Application.dll` (and Alarm equivalent) are present.

3. **Explicit assembly preload**: Try `Assembly.Load("Dialysis.Treatment.Application")` before `AddIntercessor` to rule out lazy loading.

4. **Scrutor version**: Ensure Scrutor is up to date; check for known issues with .NET 10.

### Changes Applied (IntercessorBuilder.cs)

1. **Pre-warm assemblies** – Before the Scrutor scan, call `assembly.GetTypes()` for each assembly. On `ReflectionTypeLoadException`, wrap and rethrow with loader error messages. Surfaces type-load failures (e.g. in Docker) with actionable diagnostics.
2. **publicOnly: false** – Scrutor’s `AddClasses` defaults to `publicOnly: true`, which can omit internal types. Set `publicOnly: false` so all handler types are discovered in Docker/published apps.

### Verification

Run Seeder against Docker and confirm Treatment and Alarm ingest succeed. If so, remove explicit handler registrations from Treatment and Alarm `Program.cs`.

---

## 2. Intercessor Consistency

Once assembly scan works in Docker:

1. Remove explicit `AddTransient<ICommandHandler<...>, ...>()` from:
   - `Services/Dialysis.Treatment/Dialysis.Treatment.Api/Program.cs`
   - `Services/Dialysis.Alarm/Dialysis.Alarm.Api/Program.cs`
2. Update `.cursor/rules/intercessor.mdc` to remove the "when assembly scan fails" fallback guidance (or keep as rare exception).

---

## 3. Documentation Updates

### DEPLOYMENT-RUNBOOK.md

- §3.4: Add **If Seeder fails (500)** tip: rebuild and start affected APIs:
  ```bash
  docker compose up -d --build prescription-api treatment-api alarm-api gateway
  docker compose logs prescription-api treatment-api alarm-api
  ```
- §7 Troubleshooting: Add row for "500 on HL7" covering both gateway routing and handler registration (Treatment, Alarm).

### SYSTEM-ARCHITECTURE.md

- §15 Migrations: Add **Prune and recreate** flow:
  - When to use: schema drift, broken migrations, clean slate
  - Steps: drop DBs, remove migration files, add `InitialCreate`, apply
- §2 / §16: Mention `transponder` database (Postgres init, Transponder migrations).

---

## Dependencies

- Todo `investigate-assembly-scan` → `fix-intercessor-docker`
- Todos `update-docs-seeder` and `update-docs-migrations` can run in parallel
