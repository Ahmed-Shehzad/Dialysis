## What

<!-- One paragraph: what changes and why. Link the issue if there is one. -->

## Checklist

- [ ] `dotnet build Dialysis.slnx` clean (warnings are errors) and the affected module's tests pass
- [ ] Frontend touched → `npm run lint && npm run typecheck && npm run test:unit` in each affected app; duplicated files kept byte-identical (`tools/frontend/check-duplicate-sync.sh`)
- [ ] AppHost touched → `./build.sh PublishAllCompose PublishAllKubernetes` re-run and regenerated `deploy/` committed (the drift gate fails the PR otherwise)
- [ ] Integration-event schema changed → `SchemaVersion` bumped and `IntegrationEventVersioningTests` updated
- [ ] New cross-module interaction goes through `<Module>.Contracts` + Transponder (no direct project references — the architecture tests enforce this)
- [ ] PHI/personal-data handling changed → `[PhiAccess]` audit attributes, `IPatientEraser`/`IModuleDataExtractor` coverage, and retention implications considered

## Notes for the reviewer

<!-- Risk areas, intentionally-deferred work, rollout/ops considerations. -->
