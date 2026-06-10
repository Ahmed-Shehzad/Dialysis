# SonarQube (SonarJS) analysis — React applications

**Date:** 2026-06-10
**Scope:** all seven per-module SPAs under `src/frontend/` (`his-web`, `ehr-web`, `pdms-web`, `smartconnect-web`, `hie-web`, `identity-web`, `patient-portal-web`)
**Files analyzed:** 491 source files (`src/**/*.{ts,tsx,js,jsx}`)
**Engine:** SonarJS `eslint-plugin-sonarjs` v4.0.3 (the same analyzer SonarQube/SonarCloud uses for JS/TS), recommended rule set (269 rules)

## How this was run (and why)

The repo's normal SonarQube path (`tools/sonarqube/scan.sh`) targets the **.NET solution** against the **Aspire-hosted SonarQube server** (`http://localhost:9000`, dev-only). That server is **not running in this environment**, the `dotnet-sonarscanner` CLI is not installed, and the SonarCloud MCP server needs a `SONARQUBE_TOKEN` that isn't configured here — so a server-backed dashboard scan of the SPAs wasn't possible.

To still analyze the React code with the **real SonarSource rule engine**, I ran `eslint-plugin-sonarjs` (SonarJS — the exact analyzer behind SonarQube's JavaScript/TypeScript quality profile) locally via the ESLint API, with no changes to the apps' own configs.

**Scope caveat:** this run uses SonarJS's **non-type-aware** rules (no per-app `tsconfig` project wiring, which would be far slower). A handful of SonarJS rules that require type information (some taint/security and null-dereference rules) are therefore not exercised. For the canonical Quality-Gate numbers (coverage %, duplication %, security rating), run the server scan with the AppHost up and extend `scan.sh` to include the frontend (see *Recommendations*).

## Summary

| | Count |
|---|--:|
| **Total findings** | **114** |
| Files analyzed | 491 |
| Severity: error | 114 |
| Severity: warning | 0 |
| Distinct rules triggered | 10 |
| Bugs | 0 |
| Security hotspots (need review) | 15 (`pseudo-random` ×8, `no-clear-text-protocols` ×6, `slow-regex` ×1) |
| Maintainability (code smells) | 99 |

There are **no reliability bugs**. The 15 security-hotspot hits are all **benign in context** (analysis below). The remaining 99 are maintainability code-smells, dominated by one stylistic rule (`void-use`, 50 of 114).

### Findings per app

| App | Files | Findings |
|---|--:|--:|
| ehr-web | 94 | 31 |
| pdms-web | 81 | 23 |
| patient-portal-web | 72 | 18 |
| smartconnect-web | 66 | 13 |
| hie-web | 64 | 12 |
| identity-web | 62 | 9 |
| his-web | 52 | 8 |
| **Total** | **491** | **114** |

### Findings per rule × app

| Rule | his | ehr | pdms | smartconnect | hie | identity | patient-portal | **Total** |
|---|--:|--:|--:|--:|--:|--:|--:|--:|
| `void-use` | 5 | 17 | 7 | 2 | 4 | 2 | 13 | **50** |
| `no-nested-conditional` | 1 | 5 | 5 | 5 | 1 | 1 | 1 | **19** |
| `redundant-type-aliases` | 1 | 1 | 1 | 3 | 1 | 1 | 1 | **9** |
| `no-nested-functions` | · | 3 | 2 | · | 2 | 2 | · | **9** |
| `pseudo-random` | 1 | 1 | 1 | 2 | 1 | 1 | 1 | **8** |
| `no-clear-text-protocols` | · | · | 4 | 1 | 1 | · | · | **6** |
| `cognitive-complexity` | · | 1 | 1 | · | 1 | 1 | 1 | **5** |
| `no-redundant-boolean` | · | · | 1 | · | 1 | 1 | 1 | **4** |
| `no-nested-template-literals` | · | 2 | 1 | · | · | · | · | **3** |
| `slow-regex` | · | 1 | · | · | · | · | · | **1** |
| **Total** | **8** | **31** | **23** | **13** | **12** | **9** | **18** | **114** |

## The duplication multiplier

This codebase intentionally duplicates cross-cutting files byte-for-byte across apps (see `CLAUDE.md` §Frontend). That means **one underlying issue shows up N times** in the table above. Fixing the single source and re-syncing the copies clears a cluster at once:

| Duplicated source | Rule | Apps affected |
|---|---|--:|
| `features/durable-commands/components/toastBus.ts:40` | `pseudo-random` | 7 (all) |
| `shell/types.ts:21` | `redundant-type-aliases` | 7 (all) |
| `features/documents/components/PdfViewerDrawer.tsx` (123/284/485) | `cognitive-complexity` + `no-redundant-boolean` + `no-nested-conditional` | 4 |
| `features/patients/patientLoader.ts` (27/32) | `no-nested-functions` | 4 |
| `features/notifications/useBffNotifications.ts` (55/56) | `void-use` | 3 |

Roughly **45 of the 114 findings come from ~5 duplicated files.** Prioritizing those gives the biggest cleanup per edit.

## Security hotspots — reviewed

SonarQube "Security Hotspots" are flagged for **manual review**, not confirmed vulnerabilities. All 15 here are safe in context:

- **`pseudo-random` (8)** — every hit is `Math.random().toString(36)` used to mint a **DOM/toast element id** (`toastBus.ts:40`, `NewChannelDialog.tsx:71`). Not security-sensitive; no crypto or token use. *Verdict: not a vulnerability.* If you want a clean hotspot count, switch the id generator to `crypto.randomUUID()`.
- **`no-clear-text-protocols` (6)** — all hits are FHIR **canonical system URIs** that are required to be literal `http://…` identifiers, not network endpoints: `http://www.nlm.nih.gov/research/umls/rxnorm`, `http://hl7.org/fhir/sid/ndc`, `http://www.whocc.no/atc` (PDMS `RecordAdministrationDialog`), and similar in `adapterSchemas.ts` / `FhirAuthoringPage.tsx`. These are FHIR coding-system identifiers — changing them to `https` would be **incorrect**. *Verdict: false positive; suppress with a scoped `// NOSONAR`/disable comment or a project-level exclusion.*
- **`slow-regex` (1)** — `ehr-web .../EhrChartPage.tsx:57` parses short chart values (`"120 mmHg"`) with `/^([\d./-]+)\s*(.*)$/`. Inputs are tiny, so ReDoS risk is negligible, but the `[\d./-]+\s*` adjacency is the kind of thing worth tightening. *Verdict: low risk; optionally anchor more strictly.*

## Maintainability findings — by rule

- **`void-use` (50)** — `void somePromise()` to deliberately fire-and-forget an async call (event handlers, effect cleanups, notification streams). This is a common, *intentional* React pattern. SonarJS's recommended profile flags it; the team's own ESLint config does not. **Recommendation: tune this rule off** in the frontend Sonar profile (or standardize on `.catch(() => {})`) — otherwise it dominates the count and drowns out signal.
- **`no-nested-conditional` (19)** — nested ternaries, mostly in JSX render branches (`PatientTimeline`, `BillingChargesPage`, `Hl7WorkbenchTab`). Genuine readability smell; extract to a helper/`switch` or early-return. Low effort, good payoff.
- **`redundant-type-aliases` (9)** — `type Foo = string` aliases (e.g. `shell/types.ts:21`). Inline the primitive or make it a branded type if the alias is meant to document intent.
- **`no-nested-functions` (9)** — >4 levels of function nesting, concentrated in `patientLoader.ts` (duplicated) and `AfterVisitSummaryAuthoringCard.tsx`. Hoist inner closures.
- **`cognitive-complexity` (5)** — functions over the 15 threshold: `PdfViewerDrawer.tsx:123` (duplicated ×4) and `PatientTimeline.tsx:70` (CC 23). Worth a refactor; the PdfViewer one clears 4 apps at once.
- **`no-redundant-boolean` (4)**, **`no-nested-template-literals` (3)** — small local cleanups.

## Recommended next steps

1. **Quick wins via the duplication multiplier** — refactor `PdfViewerDrawer.tsx`, `patientLoader.ts`, `toastBus.ts`, `useBffNotifications.ts`, `shell/types.ts` once and re-sync copies; clears ~45 findings.
2. **Tune the frontend Sonar profile** — turn `void-use` off (or down to info) and mark the FHIR-URI `no-clear-text-protocols` and toast-id `pseudo-random` hits as reviewed/safe. That removes ~64 noise findings and leaves ~50 actionable ones.
3. **Fix the real smells** — nested ternaries (19) and the two high cognitive-complexity functions are the highest-value maintainability work.
4. **Wire the SPAs into the canonical server scan** — extend `tools/sonarqube/scan.sh` (or add a `sonar-project.properties`) with `sonar.sources=src/frontend`, `sonar.javascript.lcov.reportPaths` from each app's Vitest coverage, and run with the AppHost up to get Quality-Gate metrics (coverage, duplication %, security rating) on the dashboard — and to exercise the type-aware rules this local run skips.

## Appendix — full findings

<!-- begin full findings -->
### his-web — 8 findings

| File:Line | Rule |
|---|---|
| `src/features/durable-commands/components/toastBus.ts:40` | pseudo-random |
| `src/features/notifications/useBffNotifications.ts:55` | void-use |
| `src/features/notifications/useBffNotifications.ts:56` | void-use |
| `src/modules/his/admin/BillingExportsPage.tsx:31` | void-use |
| `src/modules/his/admin/DeviceRegistryPage.tsx:48` | void-use |
| `src/modules/his/today/queueApi.ts:148` | void-use |
| `src/modules/his/today/QueueCard.tsx:74` | no-nested-conditional |
| `src/shell/types.ts:21` | redundant-type-aliases |

### ehr-web — 31 findings

| File:Line | Rule |
|---|---|
| `src/features/durable-commands/components/toastBus.ts:40` | pseudo-random |
| `src/features/hie/components/ConsentAdminPanel.tsx:70` | void-use |
| `src/features/hie/components/OutboundQueuePanel.tsx:49` | void-use |
| `src/features/imaging/components/ImagingPanel.tsx:62` | void-use |
| `src/features/imaging/components/ImagingPanel.tsx:69` | void-use |
| `src/features/notifications/useBffNotifications.ts:55` | void-use |
| `src/features/notifications/useBffNotifications.ts:56` | void-use |
| `src/features/patients/patientLoader.ts:22` | void-use |
| `src/features/patients/patientLoader.ts:27` | no-nested-functions |
| `src/features/patients/patientLoader.ts:32` | no-nested-functions |
| `src/modules/ehr/admin/AppointmentRequestsWorklist.tsx:67` | void-use |
| `src/modules/ehr/admin/BillingChargesPage.tsx:272` | no-nested-conditional |
| `src/modules/ehr/admin/ConditionControlPanel.tsx:110` | no-nested-conditional |
| `src/modules/ehr/chart/AddNoteDialog.tsx:56` | void-use |
| `src/modules/ehr/chart/AddNoteDialog.tsx:59` | void-use |
| `src/modules/ehr/chart/AfterVisitSummaryAuthoringCard.tsx:63` | no-nested-conditional |
| `src/modules/ehr/chart/AfterVisitSummaryAuthoringCard.tsx:110` | no-nested-functions |
| `src/modules/ehr/chart/EhrChartPage.tsx:57` | slow-regex |
| `src/modules/ehr/chart/EhrChartPage.tsx:408` | no-nested-conditional |
| `src/modules/ehr/chart/MessagingCard.tsx:50` | void-use |
| `src/modules/ehr/chart/MessagingCard.tsx:51` | void-use |
| `src/modules/ehr/chart/OrderLabsDialog.tsx:71` | void-use |
| `src/modules/ehr/chart/OrderPrescriptionDialog.tsx:81` | void-use |
| `src/modules/ehr/chart/OrderSetDialog.tsx:60` | void-use |
| `src/modules/ehr/chart/PatientTimeline.tsx:70` | cognitive-complexity |
| `src/modules/ehr/chart/PatientTimeline.tsx:90` | no-nested-template-literals |
| `src/modules/ehr/chart/PatientTimeline.tsx:113` | no-nested-conditional |
| `src/modules/ehr/chart/PatientTimeline.tsx:139` | no-nested-template-literals |
| `src/modules/ehr/chart/RecentNotesPanel.tsx:57` | void-use |
| `src/modules/ehr/chart/ReferralDialog.tsx:32` | void-use |
| `src/shell/types.ts:21` | redundant-type-aliases |

### pdms-web — 23 findings

| File:Line | Rule |
|---|---|
| `src/features/documents/components/PdfViewerDrawer.tsx:123` | cognitive-complexity |
| `src/features/documents/components/PdfViewerDrawer.tsx:284` | no-redundant-boolean |
| `src/features/documents/components/PdfViewerDrawer.tsx:485` | no-nested-conditional |
| `src/features/durable-commands/components/toastBus.ts:40` | pseudo-random |
| `src/features/medications/components/RecordAdministrationDialog.tsx:31` | no-clear-text-protocols |
| `src/features/medications/components/RecordAdministrationDialog.tsx:95` | no-clear-text-protocols |
| `src/features/medications/components/RecordAdministrationDialog.tsx:96` | no-clear-text-protocols |
| `src/features/medications/components/RecordAdministrationDialog.tsx:97` | no-clear-text-protocols |
| `src/features/notifications/useBffNotifications.ts:55` | void-use |
| `src/features/notifications/useBffNotifications.ts:56` | void-use |
| `src/features/patients/patientLoader.ts:22` | void-use |
| `src/features/patients/patientLoader.ts:27` | no-nested-functions |
| `src/features/patients/patientLoader.ts:32` | no-nested-functions |
| `src/features/vitals/audio/vitalsAlarmAudio.ts:98` | void-use |
| `src/features/vitals/components/VitalsSoundToggle.tsx:30` | no-nested-conditional |
| `src/features/vitals/hooks/useVitalsMonitorSound.ts:65` | void-use |
| `src/features/vitals/hooks/useVitalsStream.ts:84` | void-use |
| `src/modules/pdms/admin/ReportingTemplatesPage.tsx:171` | no-nested-conditional |
| `src/modules/pdms/chairs/ChairBoardPage.tsx:23` | no-nested-conditional |
| `src/modules/pdms/chairside/alarmsApi.ts:90` | void-use |
| `src/modules/pdms/chairside/ChairsideAlarmStrip.tsx:61` | no-nested-template-literals |
| `src/modules/pdms/chairside/ChairsideHeader.tsx:47` | no-nested-conditional |
| `src/shell/types.ts:21` | redundant-type-aliases |

### smartconnect-web — 13 findings

| File:Line | Rule |
|---|---|
| `src/features/durable-commands/components/toastBus.ts:40` | pseudo-random |
| `src/features/smartconnect/api/adapterSchemas.ts:227` | no-clear-text-protocols |
| `src/features/smartconnect/api/types.ts:38` | redundant-type-aliases |
| `src/features/smartconnect/api/types.ts:48` | redundant-type-aliases |
| `src/features/smartconnect/components/AdapterParametersForm.tsx:110` | no-nested-conditional |
| `src/features/smartconnect/components/NewChannelDialog.tsx:71` | pseudo-random |
| `src/features/smartconnect/components/OutboundConcurrencyTimeline.tsx:41` | no-nested-conditional |
| `src/features/smartconnect/tabs/DependencyGraphTab.tsx:186` | no-nested-conditional |
| `src/features/smartconnect/tabs/Hl7WorkbenchTab.tsx:402` | no-nested-conditional |
| `src/features/smartconnect/tabs/Hl7WorkbenchTab.tsx:404` | no-nested-conditional |
| `src/pages/ChannelEditorPage.tsx:118` | void-use |
| `src/pages/ChannelEditorPage.tsx:119` | void-use |
| `src/shell/types.ts:21` | redundant-type-aliases |

### hie-web — 12 findings

| File:Line | Rule |
|---|---|
| `src/features/documents/components/PdfViewerDrawer.tsx:123` | cognitive-complexity |
| `src/features/documents/components/PdfViewerDrawer.tsx:284` | no-redundant-boolean |
| `src/features/documents/components/PdfViewerDrawer.tsx:485` | no-nested-conditional |
| `src/features/durable-commands/components/toastBus.ts:40` | pseudo-random |
| `src/features/hie/components/ConsentAdminPanel.tsx:70` | void-use |
| `src/features/hie/components/OutboundQueuePanel.tsx:50` | void-use |
| `src/features/patients/patientLoader.ts:21` | void-use |
| `src/features/patients/patientLoader.ts:26` | no-nested-functions |
| `src/features/patients/patientLoader.ts:31` | no-nested-functions |
| `src/modules/hie/admin/MpiStewardPage.tsx:34` | void-use |
| `src/pages/FhirAuthoringPage.tsx:276` | no-clear-text-protocols |
| `src/shell/types.ts:21` | redundant-type-aliases |

### identity-web — 9 findings

| File:Line | Rule |
|---|---|
| `src/features/documents/components/PdfViewerDrawer.tsx:123` | cognitive-complexity |
| `src/features/documents/components/PdfViewerDrawer.tsx:284` | no-redundant-boolean |
| `src/features/documents/components/PdfViewerDrawer.tsx:485` | no-nested-conditional |
| `src/features/durable-commands/components/toastBus.ts:40` | pseudo-random |
| `src/features/patients/patientLoader.ts:22` | void-use |
| `src/features/patients/patientLoader.ts:27` | no-nested-functions |
| `src/features/patients/patientLoader.ts:32` | no-nested-functions |
| `src/features/vitals/hooks/useVitalsStream.ts:84` | void-use |
| `src/shell/types.ts:21` | redundant-type-aliases |

### patient-portal-web — 18 findings

| File:Line | Rule |
|---|---|
| `src/features/documents/components/PdfViewerDrawer.tsx:123` | cognitive-complexity |
| `src/features/documents/components/PdfViewerDrawer.tsx:284` | no-redundant-boolean |
| `src/features/documents/components/PdfViewerDrawer.tsx:485` | no-nested-conditional |
| `src/features/durable-commands/components/toastBus.ts:40` | pseudo-random |
| `src/features/hie/components/ConsentAdminPanel.tsx:70` | void-use |
| `src/features/hie/components/OutboundQueuePanel.tsx:49` | void-use |
| `src/features/notifications/usePatientPortalNotifications.ts:40` | void-use |
| `src/features/notifications/usePatientPortalNotifications.ts:42` | void-use |
| `src/features/notifications/usePatientPortalNotifications.ts:46` | void-use |
| `src/features/notifications/usePatientPortalNotifications.ts:59` | void-use |
| `src/features/vitals/hooks/useVitalsStream.ts:84` | void-use |
| `src/modules/patient-portal/BookAppointmentDialog.tsx:66` | void-use |
| `src/modules/patient-portal/MessagesPanel.tsx:52` | void-use |
| `src/modules/patient-portal/MessagesPanel.tsx:64` | void-use |
| `src/modules/patient-portal/MessagesPanel.tsx:77` | void-use |
| `src/modules/patient-portal/MessagesPanel.tsx:78` | void-use |
| `src/modules/patient-portal/MyAppointmentRequestsPanel.tsx:60` | void-use |
| `src/shell/types.ts:21` | redundant-type-aliases |
<!-- end full findings -->
