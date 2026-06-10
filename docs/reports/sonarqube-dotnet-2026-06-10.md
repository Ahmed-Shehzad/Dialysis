# SonarQube (SonarAnalyzer.CSharp) analysis — .NET solution

**Date:** 2026-06-10
**Scope:** the full `Dialysis.slnx` solution — every backend module, building block, BFF, host, test project, and tool (~190 projects)
**Engine:** `SonarAnalyzer.CSharp` 10.27.0.140913 (the same rule engine a SonarQube server scan executes for C#), run in-build via Roslyn against the repo's own `.editorconfig` Sonar profile

## Headline finding: the analyzer was never actually running

`SonarAnalyzer.CSharp` was pinned in `Directory.Packages.props` and `.editorconfig` carries an
~80-line curated Sonar profile — but **no project referenced the analyzer package**, so zero Sonar
rules had ever executed during `dotnet build`. The "clean" warnings-as-errors build was clean
because nothing was looking. (The package was only exercised by `tools/sonarqube/scan.sh` against
the dev-only Aspire SonarQube server, which CI does not run.)

This pass wired the analyzer in for real (`Directory.Build.props`, `PrivateAssets="all"`, same
pattern as the VS Threading analyzers) and triaged everything it surfaced. **The Sonar profile is
now enforced on every compile**, and since `TreatWarningsAsErrors=true`, any future warning-tier
Sonar finding fails the build.

A second latent defect surfaced by the first real run: both test-relaxation sections in
`.editorconfig` used the glob `**/{Tests,*.Tests,*.Tests.*}/**/*.cs`. Roslyn translates `**` to
`.*` and `/` literally, so `/**/` demands an extra path segment — the old glob never matched files
sitting at a test project's **root**, and `**/<dir>` can never match the repo-root `tests/` tree
(`Dialysis.ArchitectureTests`) at all. Both sections now use
`{tests/**.cs,**/Tests/**.cs,**/*.Tests/**.cs,**/*.Tests.*/**.cs}`.

## Result: 672 unique findings → 0 build-breaking, with every decision recorded

| | Count |
|---|--:|
| **Total unique findings (audit build)** | **672** |
| Distinct rules triggered | 43 |
| Fixed in code | ~150 |
| Newly profiled with documented justification | ~230 |
| Suggestion-tier by prior design (kept, IDE-visible) | ~280 |
| Targeted `#pragma` suppressions with justification | 17 sites |

The audit build deliberately elevated the profile's suggestion-tier rules (S6354, S3776, S3267,
S2325, S1135, S1186, S1066, S3260, S3881, S6610) to warning so they would be counted; those 281
findings return to IDE-only suggestions by design ("flag but don't block") and are inventory, not
debt to clear.

### Findings per module (audit build)

| Module | Findings | | Module | Findings |
|---|--:|---|---|--:|
| SmartConnect | 185 | | HIE | 30 |
| EHR | 154 | | tests/ | 11 |
| BuildingBlocks | 125 | | Shared | 9 |
| PDMS | 71 | | tools/ | 7 |
| HIS | 62 | | Lab / Identity / DDD / CQRS / aspire | 18 |

### Findings per rule and what was done

| Rule | What it flags | Count | Action |
|---|---|--:|---|
| S6354 | `DateTime` → prefer `DateTimeOffset` | 183 | **kept at suggestion** (contextual; documented in profile) |
| S6964 | value-type binding props can under-post | 101 | **profiled → suggestion** — the Verifier pipeline owns request validation |
| S3776 | cognitive complexity | 60 | **kept at suggestion** (prior design) |
| S3459 | unassigned auto-property | 55 | **profiled → none** — JSON/config-bound property bags; serializer assigns them |
| S6602 | `Find` instead of `FirstOrDefault` | 51 | **fixed** — 52 production sites switched (incl. one `Array.Find`); test sites covered by the repaired test-glob relaxation |
| S6605 | `Exists` instead of `Any` | 32 | **fixed** (production sites; tests relaxed by design) |
| S3267 | loops → LINQ | 31 | **kept at suggestion** (prior design) |
| S2094 | empty class/record | 29 | **profiled → none** — module marker types (`HandlerAssemblies` anchors) are the established pattern |
| S3220 | `params` overload ambiguity | 26 | **profiled → none** — OpenXml's `Append(params)` builder trips this by design |
| S4456 | split parameter-check from iterator | 10 | **fixed** — FHIR feeders split into guard + local iterator |
| S6966 | await the async API | 9 | **fixed** — Jint `Evaluate/Execute` → `EvaluateAsync/ExecuteAsync`; SSH.NET `DownloadFile` → `DownloadFileAsync` |
| S6667 | pass the caught exception to the logger | 9 | **fixed** — all 9 catch-blocks now log the exception object |
| S2139 | log-and-rethrow | 7 | **profiled → suggestion** — transport consumer loops log then rethrow deliberately |
| S1172 | unused parameter | 6 | **fixed** — includes deleting a dead `FhirJsonSerializerProvider` threading through 3 helpers and 6 handlers |
| S108 | empty block | 6 | **fixed** — intent comments added inside deliberate no-ops |
| S1481 | unused local | 5 | **fixed** (test files) |
| S6960 | controller has multiple responsibilities | 4 | **profiled → suggestion** — controllers group per workflow surface |
| S1006 | default-parameter mismatch on override | 4 | **fixed** — test fakes aligned with `IInboundMessageFactory` |
| S907 | `goto` | 3 | **suppressed with comment** — forward-only jumps to the routing-slip dispatch epilogue |
| S3885 | `Assembly.LoadFrom` | 3 | **profiled → none (tests)** — architecture tests load module assemblies by path on purpose |
| S2930 | dispose `CancellationTokenSource` | 3 | **fixed** — `using var cts` in MLLP tests |
| S2743 | static field in generic type | 3 | **fixed** — `JsonSerializerOptions` hoisted to file-scoped non-generic holders |
| S2326 | unused type parameter | 3 | **suppressed with comment** — phantom type params drive mediator/DI dispatch (`IRequest<TResponse>`, `IRequestMessage<TResponse>`, `IFhirSearcher<TResource>`) |
| S1135 | TODO tracking | 3 | **kept at suggestion** (prior design) |
| S3923 | all branches identical | 2 | **fixed** — collapsed the modality switch that always returned `["N18.6"]`; replaced a `?:` that returned `1` either way |
| S3246 | missing variance | 2 | **fixed** — `INdjsonResourceFeeder<out T>`, `ITransponderSagaMessageMutator<in T>` |
| S3011 | reflection accessibility bypass | 2 | **suppressed with comment** — documented PDFsharp internal-ctor workaround |
| S1244 | float equality | 2 | **suppressed with comment** — JS truthiness is an exact-zero test by definition |
| S2325, S1186 | static-able members, empty methods | 4 | **kept at suggestion** (prior design) |
| S6603 | `TrueForAll` instead of `All` | 2 | **fixed** (production sites) |
| S6934 / S6931 | controller route attributes | 2 | **fixed** — PDMS `ReportsController` and HIS `HealthController` get controller-level routes; URLs unchanged |
| S4830 | server cert validation disabled | 1 | **suppressed with comment** — gated behind `ForDevelopmentOnlyDisableCertificateValidation` |
| S2068 | hard-coded credential | 1 | **suppressed with comment** — throwaway passphrase for an in-memory self-signed PFX in the demo simulator |
| S3875 | `operator ==` overload | 1 | **suppressed with comment** — DDD identity equality on `AggregateRoot<TId>` |
| S1694 | abstract class → interface | 1 | **suppressed with comment** — `ParsedMessage` stays a class for future shared helpers |
| S1643 | string concat in loop | 1 | **fixed** — `StringBuilder` in `HttpFhirTerminologyService.ExpandAsync` |
| S1905 | unnecessary cast | 1 | **fixed** |
| S3928 | wrong `ArgumentException` param name | 1 | **fixed** |
| S2933 | field can be readonly | 1 | **suppressed with comment** — EF rehydrates via whole-list assignment (matches existing IDE0044 suppression) |
| S4663 | empty comment | 1 | **fixed** |
| S127 | loop counter updated in body | 1 | **suppressed with comment** — the standard CSV `""` escape lookahead-skip |

### Security review

Four findings were security-flavored; all were reviewed individually:

- **S2068 (hard-coded credential)** — `"simulator-dev-pfx"` in the data simulator is the passphrase
  for a self-signed PFX generated in-memory per run; it protects nothing persistent.
- **S4830 (cert validation disabled)** — gRPC transport only, strictly behind the
  `ForDevelopmentOnlyDisableCertificateValidation` option whose name carries the warning.
- **S3011 ×2 (accessibility bypass)** — PDFsharp's `PdfAcroForm`/field constructors are internal;
  reflection is the library's documented construction workaround, not a privilege escalation.

No exploitable finding was identified. The profile keeps the hard security rules (S2755, S4423,
S4426, S5547, S6781) at **error**, and they now actually run on every compile.

## Notable code changes beyond the mechanical

- **`FhirAuthoringEndpoints`** — an entire `FhirJsonSerializerProvider` parameter chain (resolved
  from DI in six handlers, threaded through three helpers, never used) deleted.
- **`DialysisSessionChargeReadyConsumer`** — the modality→diagnosis-pointer switch always returned
  `["N18.6"]`; collapsed with a comment explaining when to reintroduce the switch.
- **SmartConnect script engine** — five Jint `Evaluate` and two `Execute` calls in async methods
  now use the async variants (promise-returning scripts resolve properly as a bonus); the SFTP
  connector download is now truly async instead of blocking a thread-pool thread.
- **Routing controllers** — `ReportsController` (PDMS) and `HealthController` (HIS) gained
  controller-level `[Route]` prefixes with byte-identical effective URLs.

## How to re-run

```bash
dotnet build Dialysis.slnx            # the profile is enforced on every compile now
./build.sh ShowVersion Test Pack      # CI path — same analyzers, same profile
tools/sonarqube/scan.sh               # dashboard scan against the Aspire SonarQube (dev)
```

The audit itself (elevating suggestion-tier rules to see the inventory) can be repeated by
temporarily setting the S-rules listed above to `warning` in `.editorconfig` and building with
`-p:TreatWarningsAsErrors=false`.
