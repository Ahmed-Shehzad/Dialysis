# Pakistan Market Strategy — Dialysis Care Platform as SaaS

*Business-development and marketing analysis, June 2026. Companion to
`dialysis-platform-overview.html`; figures are directional estimates for planning, not
audited market research — validate during Phase 1 discovery.*

---

## 1. Why Pakistan, and why this product

Pakistan carries one of the world's heaviest kidney-disease burdens — adult CKD prevalence
estimates run above 20%, with a growing dialysis population and chronically lagging
capacity. Care delivery is fragmented across charity networks, government hospitals, and a
fast-growing private-centre segment, almost all of it run on paper registers and Excel.

The platform's architecture maps onto these realities unusually well:

| Pakistan reality | Platform answer |
|---|---|
| No AWS/Azure region in-country; institutional preference for on-premises; data-residency sensitivity | **Per-clinic deployment units** — each bounded context ships as an independently installable release; "runs in your server room" and "hosted, we manage it" come from the same codebase |
| One nephrologist supervising many chairs; stretched staffing ratios | **Live chairside monitoring + instant alarms** — the demo that sells |
| Charity networks answer to donors, not insurers | **Audit-everything pipeline** → per-patient, per-session cost & outcome reports for donors — a feature almost nobody local offers |
| Weak local trust in software vendors' data handling | **GDPR/HIPAA-grade discipline as marketing**: "exceeds local requirements" (PECA in force; the Personal Data Protection Bill still pending) |
| Power & connectivity interruptions are routine | **Durable command queue** already moves the write-durability boundary off the network — a head start on offline tolerance |

## 2. Market segments and ICP

1. **Charity / philanthropy networks** (SIUT, Indus Hospital network, Kidney Centre,
   Edhi-affiliated units) — the volume leaders; free at point of care, zakat- and
   donor-funded. Pain: throughput, chair utilization, donor accountability. Buying motion:
   relationship + board-level trust; long cycle, huge reference value.
2. **Government / teaching hospitals & provincial programs** (provincial health
   departments, PKLI) — tender-driven, slow, but one award covers many units; watch
   provincial digital-health initiatives.
3. **Private nephrology chains & standalone centres** (5–30 chairs, physician-owned, mostly
   Lahore/Karachi/Islamabad/Faisalabad) — **the classic SaaS ICP**: fast decisions, acute
   admin pain, willing to pay for utilization and safety.
4. **Insurer-paid care** — small but growing: Sehat Sahulat Programme (government health
   card) plus private insurers (EFU, Jubilee, Adamjee) and corporate panels. **None speak
   X12/837** — claims are forms, portals, and panel invoices.

**Primary ICP for entry:** the private centre segment (3), with one charity-network anchor
(1) developed in parallel for credibility.

## 3. Product-gap analysis (the honest list)

Each gap is phrased as an engineering work item; the architecture survives every one of
them — these are writers/skins, not rewrites.

| # | Gap | Work item | Size |
|---|---|---|---|
| G1 | US claim formats are dead weight (X12 837/CPT) | New billing writers in the EHR Billing slice: Sehat Sahulat claim workflow, private-insurer panel invoice (PDF/portal-shaped), plain cash receivables for out-of-pocket. Charge→Claim→Remittance lifecycle unchanged | M |
| G2 | Single-tenant deployments | **v1: unit-per-tenant hosting** (the deployment units make this operationally cheap — one Helm release set per clinic, shared cluster); true multi-tenancy deferred until tenant count justifies it | S (v1) |
| G3 | English-only UI | Urdu localization of front-desk surfaces + patient portal (clinicians stay English); RTL-aware layout pass on the affected SPAs | M |
| G4 | Patient portal assumes good devices/connectivity | **WhatsApp notification channel** (appointment reminders, report-ready, payment links) via the existing ClinicianNotification building block pattern — in Pakistan, WhatsApp will outperform the portal | S–M |
| G5 | No local payment rails | JazzCash/Easypaisa/RAAST for patient payments and for collecting the SaaS subscription itself; card penetration is low | M |
| G6 | Offline tolerance is partial | Extend the durable-command pattern across the chairside write path; define degraded-mode UX for connectivity loss | M–L |

## 4. Go-to-market

### Motion
- **Anchor-tenant strategy**: land one flagship (charity network or one provincial program)
  at cost or free; instrument it; publish the case study — "X sessions/month, Y% chair
  utilization gain, donor reports in one click". Pakistani healthcare buys on references;
  the nephrology community is small and networks through PSN (Pakistan Society of
  Nephrology) conferences.
- **Channel partners**: distributors of Fresenius / Nipro / B.Braun machines already walk
  into every dialysis centre in the country. A bundled "machine + monitoring software"
  offer is the distribution shortcut; structure a margin share.
- **Direct motion for private centres**: WhatsApp-based demo funnel (that is where SMB
  decision-makers actually respond), 30-minute remote demo led by the live-chairs board,
  2-week free pilot on the hosted tier.

### Messaging (by audience)
- **Clinic owner / administrator**: chair utilization, fewer staff-hours on paperwork,
  faster insurer/panel reimbursement.
- **Nephrologist / clinical lead**: never miss an alarm; every chair on one screen; the
  chart follows the patient.
- **Charity board / donors**: every rupee traceable — per-session cost, per-patient
  outcomes, audit-ready reporting.
- **All audiences**: built to European/US privacy discipline — exceeding, not meeting,
  local requirements.

### Marketing channels
PSN conference presence + a published pilot outcome (clinical credibility first); WhatsApp
funnels and physician WhatsApp groups; the existing pitch deck localizes almost directly —
the billing spotlight slide becomes a **donor & insurer reporting** spotlight for segment 1.

## 5. Pricing model (directional)

- **Per-chair per-month, in PKR.** Anchor the mental math to staffing costs (think: a staff
  nurse's daily wage per chair per month), not Western SaaS price points.
- **Tiers:**
  - *Basic* — scheduling, check-in, chart.
  - *Clinical* — + live monitoring, alarms, MAR, session reports.
  - *Enterprise* — + billing/panel invoicing, donor reporting, record exchange, on-prem
    option.
- **Charity tier** — deeply discounted or free for verified charity networks: part CSR,
  part strategy (their volume hardens the product; their name sells it).
- **Hosting split**: hosted (we run it, unit-per-tenant) vs on-premises licence +
  support contract (charity/government preference).

## 6. Sequencing

| Phase | Timeline | Contents | Exit criterion |
|---|---|---|---|
| **1 — Localize & pilot** | 3–4 months | G1 (cash + panel invoices), G3 (Urdu), G4 (WhatsApp); hosted pilots with 2–3 private centres in Lahore/Karachi | Two paying centres; pilot metrics collected |
| **2 — Anchor & prove** | +4–6 months | Charity-network anchor on-prem deployment (the unit charts are ready today); donor-reporting feature; published case study | Reference customer presenting at PSN |
| **3 — Scale & integrate** | +6–12 months | Sehat Sahulat claim integration; distributor channel live; G5 payments, G6 offline hardening | Channel-sourced pipeline; >20 centres |

The HIE/FHIR capability stays in the back pocket: Pakistan has no operational national HIE
yet, but being *ready* is the differentiator when provincial digital-health programs tender.

## 7. Risks and counters

| Risk | Counter |
|---|---|
| Price sensitivity collapses ARPU | Per-chair pricing scales with the customer's own revenue unit; charity tier converts volume into product hardening rather than revenue |
| Local HMIS incumbents bundle "good enough" modules | None offer live chairside monitoring or donor-grade auditability — keep the demo clinical |
| Currency volatility (PKR) on hosted COGS | Host on local DCs / nearest region with costs hedged in the subscription terms; on-prem tier is naturally immune |
| Regulatory drift (data-protection bill enactment) | Already exceeding the draft's requirements; track enactment, advertise compliance day one |
| Single-anchor dependence | Run segment 3 (private centres) in parallel from day one — the anchor is for credibility, not revenue |

## 8. Immediate next actions

1. Validate the market numbers above with primary research (centre counts per city,
   per-session economics, Sehat Sahulat dialysis coverage terms).
2. Scope G1+G3+G4 as a costed engineering package ("Pakistan localization pack").
3. Identify 5 candidate pilot centres (Lahore/Karachi) and 1 anchor-candidate charity
   network; open conversations through nephrologist introductions, not cold outreach.
4. Localize the pitch deck: replace the billing spotlight with donor & insurer reporting;
   add an Urdu title variant.
