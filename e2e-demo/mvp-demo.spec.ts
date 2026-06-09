import { test, expect, type Page } from "@playwright/test";

/**
 * Full-system MVP demo — one continuous, recorded walkthrough of the entire Dialysis platform.
 *
 * This is NOT a mocked test. It drives the **live Aspire stack** through the real edge Gateway
 * (`http://localhost:9090`): it logs in once through the real Keycloak realm (`demo`/`demo`), then
 * tours every SPA + BFF (HIS, EHR, PDMS, SmartConnect, HIE, Admin/Identity, Patient Portal) over
 * real, DataSimulator-seeded data — list pages, detail drill-ins, the chairside live session, the
 * branded PDF document viewer, billing, compliance dashboards, and more.
 *
 * Captions are overlaid on every stop (the demo has no audio, so the banner narrates what you are
 * looking at). The run is paced deliberately and a tail guard keeps it running until it clears
 * 20 minutes, so the single recorded video is a self-contained MVP demo.
 *
 * Prerequisite: the stack must already be up — `dotnet run --project src/aspire/Dialysis.AppHost`.
 */

const USER = process.env.DEMO_USER ?? "demo";
const PASS = process.env.DEMO_PASS ?? "demo";
const DWELL = Number(process.env.DEMO_DWELL_MS ?? 24_000); // default per-stop dwell
const MIN_RUN_MS = Number(process.env.DEMO_MIN_MS ?? 20.5 * 60_000); // keep recording past 20 min

type Stop = {
  title: string;
  subtitle: string;
  url: string;
  dwellMs?: number;
  /** Optional substring the page should surface — recorded as a soft pass/fail, never aborts. */
  look?: string;
  /** Optional interaction (drill into a row, open a drawer, switch a tab). Defensive. */
  action?: (page: Page) => Promise<void>;
};

const results: { title: string; ok: boolean; note: string }[] = [];

/**
 * Drives the real auth flow until the app renders: an SPA "Sign in" launcher (`/{ctx}/login`) starts
 * the OIDC challenge, the Keycloak form (`/auth/realms/…`, first context only — SSO thereafter) takes
 * `demo`/`demo`, and any non-login/non-Keycloak URL means we're authenticated. A short settle before
 * each decision lets the redirect/render complete (the reliable pattern; polling without it races).
 */
async function signInIfNeeded(page: Page): Promise<void> {
  for (let i = 0; i < 7; i++) {
    await page.waitForTimeout(1800);
    const u = new URL(page.url());
    const onKc =
      u.host.includes("8081") || u.pathname.includes("/realms/") || u.pathname.includes("/auth/");
    const onLogin = /\/login\/?$/.test(u.pathname);

    if (onKc) {
      const kc = page.locator("#username, input[name=username]").first();
      if (await kc.isVisible().catch(() => false)) {
        await kc.fill(USER);
        await page.locator("#password, input[name=password]").first().fill(PASS);
        await page
          .locator("#kc-login, button[type=submit], input[type=submit]")
          .first()
          .click()
          .catch(() => page.keyboard.press("Enter"));
        await page.waitForLoadState("load").catch(() => {});
      }
      continue;
    }
    if (onLogin) {
      const launcher = page
        .getByRole("button", { name: /sign in|log ?in|continue|single sign/i })
        .or(page.getByRole("link", { name: /sign in|log ?in|continue|single sign/i }))
        .first();
      if (await launcher.isVisible().catch(() => false)) {
        await launcher.click().catch(() => {});
        await page.waitForLoadState("load").catch(() => {});
      }
      continue;
    }
    return; // genuinely on an app route → authenticated
  }
}

/** Normalised pathname for comparing the landed route against the intended one. */
const samePath = (a: string, b: string) =>
  new URL(a, "http://x").pathname.replace(/\/$/, "") === new URL(b, "http://x").pathname.replace(/\/$/, "");

/** Overlays a branded caption banner (re-injected after every navigation, since the DOM is wiped). */
async function caption(page: Page, title: string, subtitle: string, path: string): Promise<void> {
  await page
    .evaluate(
      ({ title, subtitle, path }) => {
        let el = document.getElementById("__demo_caption__");
        if (!el) {
          el = document.createElement("div");
          el.id = "__demo_caption__";
          el.style.cssText =
            "position:fixed;z-index:2147483647;left:0;right:0;bottom:0;padding:10px 20px;" +
            "background:linear-gradient(90deg,#015941,#00a97a);color:#fff;" +
            "font:600 15px/1.35 system-ui,-apple-system,sans-serif;display:flex;gap:14px;" +
            "align-items:baseline;box-shadow:0 -2px 16px rgba(0,0,0,.28);pointer-events:none";
          document.body.appendChild(el);
        }
        el.innerHTML =
          `<span style="font-weight:800;letter-spacing:.2px">${title}</span>` +
          `<span style="opacity:.9;font-weight:500">${subtitle}</span>` +
          `<span style="margin-left:auto;opacity:.7;font:600 12px ui-monospace,monospace">${path}</span>`;
      },
      { title, subtitle, path },
    )
    .catch(() => {});
}

/** Gentle scroll-and-pause so the recording shows the page content over the dwell window. */
async function tour(page: Page, ms: number): Promise<void> {
  const segments = Math.max(3, Math.round(ms / 3500));
  for (let i = 0; i < segments; i++) {
    await page.mouse.wheel(0, 320).catch(() => {});
    await page.waitForTimeout(Math.round(ms / segments));
  }
  await page.evaluate(() => window.scrollTo({ top: 0, behavior: "smooth" })).catch(() => {});
  await page.waitForTimeout(800);
}

/** Clicks the first element matching a selector if present; returns whether it clicked. */
async function clickFirst(page: Page, selector: string): Promise<boolean> {
  const el = page.locator(selector).first();
  if (await el.isVisible().catch(() => false)) {
    await el.click().catch(() => {});
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(1200);
    return true;
  }
  return false;
}

/** Navigate (full-page hop — required for cross-context), authenticate, caption, verify, interact, dwell. */
async function visit(page: Page, stop: Stop): Promise<void> {
  const t0 = Date.now();
  console.log(`▶ ${stop.title} — ${stop.url}`);
  await page.goto(stop.url, { waitUntil: "domcontentloaded" }).catch(() => {});
  await signInIfNeeded(page);
  // The first OIDC round-trip into a context drops the deep link and lands on the SPA index — once
  // authenticated, navigate to the intended route directly.
  if (!samePath(page.url(), stop.url)) {
    await page.goto(stop.url, { waitUntil: "domcontentloaded" }).catch(() => {});
    await signInIfNeeded(page);
  }
  await page.waitForLoadState("networkidle").catch(() => {});
  await page.waitForTimeout(1200);
  await caption(page, stop.title, stop.subtitle, stop.url);

  let ok: boolean;
  let note: string;
  if (stop.look) {
    ok = await page
      .getByText(stop.look, { exact: false })
      .first()
      .isVisible()
      .catch(() => false);
    note = ok ? `saw "${stop.look}"` : `"${stop.look}" not visible`;
  } else {
    // Minimal invariant: the SPA shell mounted with real content (not a blank/error frame).
    const text = (await page.locator("body").innerText().catch(() => "")) || "";
    ok = text.trim().length > 40;
    note = ok ? "shell mounted" : "empty frame";
  }
  results.push({ title: stop.title, ok, note });

  if (stop.action) {
    await caption(page, stop.title, stop.subtitle, stop.url);
    await stop.action(page).catch((e) => console.log("  · action skipped:", String(e).split("\n")[0]));
    await caption(page, stop.title, stop.subtitle, stop.url);
  }
  await tour(page, stop.dwellMs ?? DWELL);
  console.log(`  ✓ ${stop.title} (${Math.round((Date.now() - t0) / 1000)}s, ${note})`);
}

// ── The tour ────────────────────────────────────────────────────────────────────────────────────
const stops: Stop[] = [
  // HIS — facility operations
  { title: "HIS · Today", subtitle: "Facility operations dashboard — staff, chairs, inventory, billing queue", url: "/his/today" },
  { title: "HIS · Workflows", subtitle: "Reference-architecture capabilities & guided clinical workflows", url: "/his/workflows" },
  {
    title: "HIS · Billing exports",
    subtitle: "Queue billing-export jobs → Execute hands off to EHR for claim filing",
    url: "/his/admin/billing/exports",
    action: async (p) => {
      await clickFirst(p, 'button:has-text("Execute"), [data-testid*="execute"]');
    },
  },
  { title: "HIS · Device registry", subtitle: "RPM device lifecycle — register, bind to patient, ingest telemetry", url: "/his/admin/devices" },

  // EHR — clinical record & billing
  { title: "EHR · Patients", subtitle: "Patient index across the record (DataSimulator-seeded)", url: "/ehr/patients" },
  {
    title: "EHR · Patient chart",
    subtitle: "Encounters, notes, orders, diagnoses, referrals — the longitudinal chart",
    url: "/ehr/patients",
    action: async (p) => {
      await clickFirst(p, 'a[href*="/ehr/patients/"]');
    },
  },
  { title: "EHR · Workflows", subtitle: "Guided clinical-documentation workflows", url: "/ehr/workflows" },
  { title: "EHR · Dialysis charges", subtitle: "Charge capture → claim lifecycle (837/277CA/999)", url: "/ehr/admin/billing/dialysis-charges" },
  { title: "EHR · Fee schedule", subtitle: "CPT fee-schedule entries driving charge pricing", url: "/ehr/admin/billing/fee-schedule" },
  { title: "EHR · Care coordination", subtitle: "Cross-team worklist", url: "/ehr/care-coordination/worklist" },
  { title: "EHR · Appointment requests", subtitle: "Patient-portal appointment requests — approve / decline", url: "/ehr/appointment-requests" },
  { title: "EHR · Population quality", subtitle: "Quality-measure evaluation across the population", url: "/ehr/population/quality" },
  { title: "EHR · Safety surveillance", subtitle: "Clinical-safety surveillance signals", url: "/ehr/safety/surveillance" },

  // PDMS — chairside dialysis (TimescaleDB telemetry)
  { title: "PDMS · Sessions", subtitle: "Dialysis treatment sessions — a machine cycle observed through telemetry", url: "/pdms/sessions" },
  {
    title: "PDMS · Live session",
    subtitle: "Chairside live view — intradialytic vitals ticking, alarms, MAR, documents",
    url: "/pdms/sessions",
    dwellMs: 34_000,
    action: async (p) => {
      await clickFirst(p, 'a[href*="/pdms/sessions/"]');
      // surface the Documents tab (session reports + branded invoice)
      await clickFirst(p, 'button:has-text("Documents"), [role=tab]:has-text("Documents")');
    },
  },
  { title: "PDMS · Chair board", subtitle: "Floor-wide chair occupancy overview", url: "/pdms/chairs" },
  { title: "PDMS · Inventory", subtitle: "Consumables & equipment inventory movements", url: "/pdms/admin/inventory" },
  { title: "PDMS · Reporting templates", subtitle: "Mustache-templated discharge letters / shift / billing docs", url: "/pdms/admin/reporting/templates" },
  { title: "PDMS · On-call rotation", subtitle: "On-call staff rotation schedule", url: "/pdms/admin/oncall/rotation" },
  { title: "PDMS · Escalation policies", subtitle: "Alarm escalation policies feeding the multi-channel clinician pager", url: "/pdms/admin/oncall/policies" },
  { title: "PDMS · On-call audit", subtitle: "Per-attempt alarm-dispatch audit trail", url: "/pdms/admin/oncall/audit" },

  // SmartConnect — integration engine
  { title: "SmartConnect · Integrations", subtitle: "Mirth-style channel/flow integration engine — HL7v2 / FHIR / DICOM", url: "/smartconnect/integrations" },
  {
    title: "SmartConnect · Channel editor",
    subtitle: "Visual flow editor for an integration channel",
    url: "/smartconnect/integrations",
    action: async (p) => {
      await clickFirst(p, 'a[href*="/smartconnect/integrations/editor/"]');
    },
  },

  // HIE — FHIR R4 / IHE exchange + branded documents
  { title: "HIE · FHIR exchange", subtitle: "Outbound queue, inbound feed, partner status, consent & community records", url: "/hie/fhir-exchange" },
  { title: "HIE · FHIR authoring", subtitle: "Author FHIR resources / subscription topics", url: "/hie/fhir-authoring" },
  { title: "HIE · Subscriptions", subtitle: "Active FHIR subscriptions with a live stream", url: "/hie/subscriptions" },
  {
    title: "HIE · Documents",
    subtitle: "DocumentReference board → branded PDF viewer (AcroForm edit + PAdES signing)",
    url: "/hie/admin/documents",
    dwellMs: 34_000,
    action: async (p) => {
      // Open the first document in the PDF viewer drawer — the corporate template + AcroForm surface.
      const opened = await clickFirst(p, 'button:has-text("Open")');
      if (opened) await p.waitForTimeout(2500);
    },
  },
  { title: "HIE · Document retention", subtitle: "GDPR Art. 5(1)(e) retention policies per document kind", url: "/hie/admin/documents/retention" },
  { title: "HIE · TEFCA partners", subtitle: "QHIN onboarding — trust anchors, mTLS, IAS-JWT minting", url: "/hie/admin/tefca/partners" },
  { title: "HIE · MPI steward", subtitle: "Master-patient-index duplicate review queue", url: "/hie/admin/mpi/reviews" },
  { title: "HIE · Terminology", subtitle: "Code-system / terminology authoring", url: "/hie/admin/terminology" },

  // Admin / Identity console
  { title: "Admin · Hub", subtitle: "Identity & governance console home", url: "/admin/" },
  { title: "Admin · Identity", subtitle: "Users, roles, permission catalog → SPA permission gates", url: "/admin/identity" },
  { title: "Admin · HIPAA", subtitle: "HIPAA Security-Rule safeguard registry (live checks)", url: "/admin/hipaa" },
  { title: "Admin · RoPA", subtitle: "GDPR Record of Processing Activities", url: "/admin/data-protection/ropa" },
  { title: "Admin · Consents", subtitle: "Consent management", url: "/admin/data-protection/consents" },
  { title: "Admin · Data-subject rights", subtitle: "Art. 15 export & Art. 17 erasure approve-and-execute pipeline", url: "/admin/data-protection/data-subject-rights" },
  { title: "Admin · Demo control", subtitle: "Demo / system control panel", url: "/admin/demo" },

  // Patient portal
  { title: "Portal · Patient self-service", subtitle: "Aggregated HIS + EHR/PDMS/HIE patient-facing reads", url: "/portal/" },
];

test.describe("Dialysis — full-system MVP demo", () => {
  test("end-to-end walkthrough of every SPA + BFF (live stack)", async ({ page }) => {
    const started = Date.now();

    // First navigation triggers the real Keycloak login; signInIfNeeded handles the form.
    for (const stop of stops) {
      await visit(page, stop);
    }

    // Tail guard: keep the recording running until it comfortably clears 20 minutes by revisiting
    // a rotation of "hero" stops, so the deliverable is always a >= 20-minute video.
    const heroes = stops.filter((s) =>
      /Today|Live session|Documents|Patients|FHIR exchange|HIPAA/.test(s.title),
    );
    let h = 0;
    while (Date.now() - started < MIN_RUN_MS) {
      const left = Math.round((MIN_RUN_MS - (Date.now() - started)) / 1000);
      console.log(`⏳ extending to clear 20 min — ${left}s remaining`);
      await visit(page, { ...heroes[h % heroes.length], subtitle: "Recap — keeping the demo running past 20 minutes" });
      h++;
    }

    // Report what rendered. Tolerant: the demo's job is to exercise the live system on video; we
    // only fail if the platform was broadly unreachable (most stops blank).
    const ok = results.filter((r) => r.ok).length;
    const total = results.length;
    const elapsedMin = ((Date.now() - started) / 60_000).toFixed(1);
    console.log(`\n=== MVP demo summary: ${ok}/${total} stops rendered, ${elapsedMin} min recorded ===`);
    for (const r of results) console.log(`  ${r.ok ? "✓" : "✗"} ${r.title} — ${r.note}`);

    expect(Date.now() - started, "recording should exceed 20 minutes").toBeGreaterThan(20 * 60_000);
    expect(ok, `at least half of ${total} stops should render`).toBeGreaterThanOrEqual(Math.ceil(total / 2));
  });
});
