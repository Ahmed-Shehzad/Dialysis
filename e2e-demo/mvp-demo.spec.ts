import { test, expect, type Page } from "@playwright/test";

/**
 * Full-system MVP demo — a scenario-driven patient-journey FILM recorded against the live stack.
 *
 * This is not a route tour. It enacts the clinical scenario "The Missed Session That Almost Became a
 * Hospitalization" (patient Marcus Bell, 58, ESKD on in-center hemodialysis) as a guided, captioned
 * walkthrough: a branded title card, act dividers, and scenes that drive the REAL SPAs + BFFs through
 * the Gateway over real DataSimulator data. One login (real Keycloak), then the story plays out across
 * HIS / EHR / PDMS / SmartConnect / HIE / Admin / Patient Portal. The video has no audio — a two-row
 * caption narrates every beat (patient context + act on top, the scene narration + a data chip below).
 *
 * The 20-minute cap is intentionally lifted; the story is paced to be watchable, however long it runs.
 *
 * Prerequisite: the stack must already be up — `dotnet run --project src/aspire/Dialysis.AppHost`.
 */

const USER = process.env.DEMO_USER ?? "demo";
const PASS = process.env.DEMO_PASS ?? "demo";
const DWELL = Number(process.env.DEMO_DWELL_MS ?? 26_000); // default per-scene dwell
const CARD_MS = Number(process.env.DEMO_CARD_MS ?? 7_500); // interstitial card dwell

const PATIENT = "Marcus Bell · 58 · ESKD (CKD 5) · in-center HD 3×/wk";
let currentAct = "";

const results: { title: string; ok: boolean }[] = [];

// ── Auth ──────────────────────────────────────────────────────────────────────────────────────
async function signInIfNeeded(page: Page): Promise<void> {
  for (let i = 0; i < 10; i++) {
    await page.waitForTimeout(1200);
    const path = new URL(page.url()).pathname;
    const onKc =
      new URL(page.url()).host.includes("8081") ||
      path.includes("/realms/") ||
      path.includes("/auth/");
    // The BFF login/callback endpoints (/{ctx}/identity/login|signin-oidc) also end in "login" — they
    // are an in-flight OIDC redirect, NOT the SPA login page, so don't treat them as a click target.
    const onBffAuth = path.includes("/identity/");
    const onSpaLogin = /\/login\/?$/.test(path) && !onBffAuth;

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
        await page.waitForLoadState("load", { timeout: 8000 }).catch(() => {});
      }
      continue;
    }
    if (onSpaLogin) {
      const launcher = page
        .getByRole("button", { name: /sign in|log ?in|continue|single sign/i })
        .or(page.getByRole("link", { name: /sign in|log ?in|continue|single sign/i }))
        .first();
      if (await launcher.isVisible().catch(() => false)) {
        await launcher.click().catch(() => {});
        await page.waitForLoadState("load", { timeout: 8000 }).catch(() => {});
      }
      continue;
    }
    if (onBffAuth) continue; // mid-OIDC redirect — wait for it to land
    return; // on an app route → authenticated
  }
}

const samePath = (a: string, b: string) =>
  new URL(a, "http://x").pathname.replace(/\/$/, "") === new URL(b, "http://x").pathname.replace(/\/$/, "");

// ── Captions & cards ──────────────────────────────────────────────────────────────────────────
/** Two-row caption overlay (re-injected after each navigation; the DOM is wiped on nav). */
async function caption(page: Page, n: number, title: string, beat: string, chip: string): Promise<void> {
  await page
    .evaluate(
      ({ n, title, beat, chip, patient, act }) => {
        let el = document.getElementById("__demo_caption__");
        if (!el) {
          el = document.createElement("div");
          el.id = "__demo_caption__";
          el.style.cssText =
            "position:fixed;z-index:2147483647;left:0;right:0;bottom:0;color:#fff;" +
            "font-family:system-ui,-apple-system,sans-serif;box-shadow:0 -2px 18px rgba(0,0,0,.30);pointer-events:none";
          document.body.appendChild(el);
        }
        el.innerHTML =
          `<div style="background:#0f172a;padding:5px 20px;font-size:12px;font-weight:600;letter-spacing:.3px;` +
          `display:flex;gap:12px;opacity:.95"><span style="color:#34d399">Dialysis Platform · MVP Demo</span>` +
          `<span style="color:#94a3b8">Patient: ${patient}</span>` +
          `<span style="margin-left:auto;color:#fbbf24;text-transform:uppercase">${act}</span></div>` +
          `<div style="background:linear-gradient(90deg,#015941,#00a97a);padding:11px 20px;display:flex;gap:14px;align-items:baseline">` +
          `<span style="font-weight:800;font-size:15px;white-space:nowrap">${n}. ${title}</span>` +
          `<span style="font-weight:500;font-size:14px;opacity:.95">${beat}</span>` +
          (chip
            ? `<span style="margin-left:auto;white-space:nowrap;background:rgba(255,255,255,.16);` +
              `padding:3px 10px;border-radius:99px;font:700 12px ui-monospace,monospace">${chip}</span>`
            : "") +
          `</div>`;
      },
      { n, title, beat, chip, patient: PATIENT, act: currentAct },
    )
    .catch(() => {});
}

/** Full-screen branded interstitial (title card / act divider / closing card). */
async function showCard(
  page: Page,
  opts: { tag: string; title: string; lines: string[]; accent?: string },
): Promise<void> {
  await page.goto("about:blank").catch(() => {});
  const accent = opts.accent ?? "#00a97a";
  await page.setContent(
    `<!doctype html><html><body style="margin:0;height:100vh;display:flex;align-items:center;justify-content:center;` +
      `background:radial-gradient(1200px 600px at 50% 30%, #0b3b30, #020617);font-family:system-ui,-apple-system,sans-serif;color:#fff">` +
      `<div style="text-align:center;max-width:1100px;padding:40px">` +
      `<div style="display:inline-flex;align-items:center;gap:12px;margin-bottom:28px">` +
      `<div style="width:46px;height:46px;border-radius:12px;background:${accent};display:flex;align-items:center;justify-content:center;` +
      `font-size:24px">💧</div><div style="font-weight:800;font-size:20px;letter-spacing:.5px">DIALYSIS PLATFORM</div></div>` +
      `<div style="color:${accent};font-weight:700;letter-spacing:3px;font-size:14px;text-transform:uppercase;margin-bottom:14px">${opts.tag}</div>` +
      `<div style="font-weight:800;font-size:40px;line-height:1.15;margin-bottom:22px">${opts.title}</div>` +
      opts.lines
        .map((l) => `<div style="font-size:18px;color:#cbd5e1;margin:6px 0;line-height:1.4">${l}</div>`)
        .join("") +
      `</div></body></html>`,
  );
  await page.waitForTimeout(opts.tag === "MVP Demonstration" ? CARD_MS + 3500 : CARD_MS);
}

// ── Scene mechanics ─────────────────────────────────────────────────────────────────────────────
async function tour(page: Page, ms: number): Promise<void> {
  const segments = Math.max(3, Math.round(ms / 3500));
  for (let i = 0; i < segments; i++) {
    await page.mouse.wheel(0, 300).catch(() => {});
    await page.waitForTimeout(Math.round(ms / segments));
  }
  await page.evaluate(() => window.scrollTo({ top: 0, behavior: "smooth" })).catch(() => {});
  await page.waitForTimeout(700);
}

async function clickFirst(page: Page, selector: string): Promise<boolean> {
  const el = page.locator(selector).first();
  if (await el.isVisible().catch(() => false)) {
    await el.click().catch(() => {});
    await page.waitForLoadState("networkidle", { timeout: 6000 }).catch(() => {});
    await page.waitForTimeout(1500);
    return true;
  }
  return false;
}

type Scene = {
  n: number;
  title: string;
  beat: string;
  chip?: string;
  url: string;
  dwellMs?: number;
  action?: (page: Page) => Promise<void>;
};

async function scene(page: Page, s: Scene): Promise<void> {
  const t0 = Date.now();
  console.log(`▶ ${s.n}. ${s.title} — ${s.url}`);
  await page.goto(s.url, { waitUntil: "domcontentloaded" }).catch(() => {});
  await signInIfNeeded(page);
  if (!samePath(page.url(), s.url)) {
    await page.goto(s.url, { waitUntil: "domcontentloaded" }).catch(() => {});
    await signInIfNeeded(page);
  }
  await page.waitForLoadState("networkidle", { timeout: 5000 }).catch(() => {});
  await page.waitForTimeout(1100);
  await caption(page, s.n, s.title, s.beat, s.chip ?? "");

  const text = (await page.locator("body").innerText().catch(() => "")) || "";
  results.push({ title: s.title, ok: text.trim().length > 40 });

  if (s.action) {
    await s.action(page).catch((e) => console.log("  · action skipped:", String(e).split("\n")[0]));
    await caption(page, s.n, s.title, s.beat, s.chip ?? "");
  }
  await tour(page, s.dwellMs ?? DWELL);
  console.log(`  ✓ ${s.n}. ${s.title} (${Math.round((Date.now() - t0) / 1000)}s)`);
}

// ── The film ──────────────────────────────────────────────────────────────────────────────────
test.describe("Dialysis — MVP demo (scenario film)", () => {
  test("The Missed Session That Almost Became a Hospitalization", async ({ page }) => {
    test.setTimeout(60 * 60 * 1000);
    const started = Date.now();

    await showCard(page, {
      tag: "MVP Demonstration",
      title: "The Missed Session That Almost Became a Hospitalization",
      lines: [
        "A real end-to-end patient journey across the Dialysis digital health ecosystem",
        "<b style='color:#fff'>Marcus Bell</b> · 58 · ESKD (CKD stage 5) · in-center hemodialysis 3×/week",
        "<span style='color:#94a3b8'>HIS · EHR · PDMS · SmartConnect · HIE · Admin · Patient Portal — live, no mocks</span>",
      ],
    });

    const acts: { act: string; tag: string; title: string; lines: string[]; scenes: Scene[] }[] = [
      {
        act: "Act I · The Missed Session",
        tag: "Act I",
        title: "The Missed Session",
        lines: ["Wednesday — Marcus misses his treatment.", "A transportation conflict no one sees coming… until the platform does."],
        scenes: [
          { n: 1, title: "The floor that morning", beat: "Wednesday: the center is running its shifts — but chair 7 is empty. Marcus is a no-show.", chip: "Wed · missed", url: "/his/today" },
          { n: 2, title: "Missed appointment detected", beat: "Scheduling flags the missed in-center session and opens a follow-up task.", chip: "no-show", url: "/ehr/appointment-requests" },
          { n: 3, title: "Care-coordination worklist", beat: "Marcus surfaces on the outreach worklist before the gap becomes dangerous.", url: "/ehr/care-coordination/worklist" },
          { n: 4, title: "AI risk rising", beat: "CDS lifts his missed-treatment and hyperkalemia risk — a 5-day gap is forming.", chip: "missed-tx risk ↑", url: "/ehr/safety/surveillance" },
        ],
      },
      {
        act: "Act II · Outreach & Telehealth",
        tag: "Act II",
        title: "Reaching Out",
        lines: ["The platform closes the loop with the patient — before the ED has to."],
        scenes: [
          { n: 5, title: "Patient portal outreach", beat: "Marcus gets a secure message + education nudge; he can request a make-up slot from his phone.", chip: "portal push", url: "/portal/" },
          { n: 6, title: "Make-up session approved", beat: "The scheduler approves a Friday make-up appointment — the request flows back through the BFF.", chip: "Fri booked", url: "/ehr/appointment-requests" },
          { n: 7, title: "Population & quality view", beat: "Adherence and quality measures track the at-risk cohort Marcus now belongs to.", url: "/ehr/population/quality" },
        ],
      },
      {
        act: "Act III · The Return",
        tag: "Act III",
        title: "Friday: He Comes Back Heavy",
        lines: ["After a ~5-day gap: +3.8 kg over dry weight, potassium 6.6, short of breath."],
        scenes: [
          { n: 8, title: "The chart on arrival", beat: "Pre-HD labs are sobering: K⁺ 6.6 mmol/L, Hgb 9.6, +3.8 kg over dry weight, BP 168/92.", chip: "K⁺ 6.6 · +3.8 kg", url: "/ehr/patients", action: async (p) => { await clickFirst(p, 'a[href*="/ehr/patients/"]'); } },
          { n: 9, title: "Today's treatment board", beat: "His make-up session is queued on the chairside board.", url: "/pdms/sessions" },
          { n: 10, title: "On the machine", beat: "Chairside live view: intradialytic vitals stream in real time; the MAR and documents sit beside them.", chip: "UF goal 3.8 L · Qb 400", dwellMs: 34_000, url: "/pdms/sessions", action: async (p) => { await clickFirst(p, 'a[href*="/pdms/sessions/"]'); await clickFirst(p, 'button:has-text("Documents"), [role=tab]:has-text("Documents")'); } },
        ],
      },
      {
        act: "Act IV · The Emergency",
        tag: "Act IV",
        title: "Intradialytic Hypotension",
        lines: ["90 minutes in, pulling fluid fast, his pressure falls off a cliff: 78/44."],
        scenes: [
          { n: 11, title: "The alarm fires", beat: "BP nadir 78/44, cramping, near-syncope. PDMS raises a treatment alarm the instant telemetry crosses threshold.", chip: "BP 78/44 ⚠", dwellMs: 32_000, url: "/pdms/sessions", action: async (p) => { await clickFirst(p, 'a[href*="/pdms/sessions/"]'); } },
          { n: 12, title: "Escalation policy", beat: "The escalation policy decides who to page, in what order, and how fast.", url: "/pdms/admin/oncall/policies" },
          { n: 13, title: "The nephrologist is paged", beat: "On-call is paged via ClinicianNotification (SMS/iOS/Android); every attempt + acknowledgement is audited.", chip: "paged · ack'd", url: "/pdms/admin/oncall/audit" },
        ],
      },
      {
        act: "Act V · Interoperability",
        tag: "Act V",
        title: "The Outside Record",
        lines: ["The team needs context fast — the network delivers it."],
        scenes: [
          { n: 14, title: "Pulling the outside record", beat: "HIE retrieves Marcus's recent outside ED encounter as US Core FHIR and reconciles medications.", chip: "FHIR R4 · US Core", url: "/hie/fhir-exchange" },
          { n: 15, title: "Lab result inbound", beat: "His repeat BMP arrives from the lab as an HL7 v2 ORU and is routed by the integration engine.", chip: "HL7 ORU^R01", url: "/smartconnect/integrations" },
          { n: 16, title: "Signed clinical document", beat: "A branded, AcroForm-enabled clinical PDF — fillable and PAdES-signed — is exchanged on the network.", chip: "PAdES signed", dwellMs: 30_000, url: "/hie/admin/documents", action: async (p) => { await clickFirst(p, 'button:has-text("Open")'); await p.waitForTimeout(2500); } },
        ],
      },
      {
        act: "Act VI · Revenue Cycle",
        tag: "Act VI",
        title: "Getting Paid, Cleanly",
        lines: ["The treatment that just saved an admission also has to bill correctly."],
        scenes: [
          { n: 17, title: "Billing export", beat: "The completed HD session is queued for billing; Execute hands it to the EHR to file the claim.", chip: "CPT 90935", url: "/his/admin/billing/exports", action: async (p) => { await clickFirst(p, 'button:has-text("Execute"), [data-testid*="execute"]'); } },
          { n: 18, title: "Charge → claim", beat: "Charge capture builds an EDI 837 claim with charge-edit checks; Medicare is primary.", chip: "EDI 837", url: "/ehr/admin/billing/dialysis-charges" },
          { n: 19, title: "Fee schedule", beat: "CPT fee-schedule entries price the encounter consistently.", url: "/ehr/admin/billing/fee-schedule" },
        ],
      },
      {
        act: "Act VII · Discharge & Home",
        tag: "Act VII",
        title: "Home — Safer Than He Left",
        lines: ["Stabilized, discharged, and now monitored between visits."],
        scenes: [
          { n: 20, title: "Discharge documentation", beat: "A discharge letter / After-Visit Summary is generated from a template.", chip: "AVS", url: "/pdms/admin/reporting/templates" },
          { n: 21, title: "The patient's copy", beat: "Marcus sees his visit summary and next steps in the portal — engaged, not in the dark.", url: "/portal/" },
          { n: 22, title: "Remote monitoring enrolled", beat: "An RPM home BP cuff is registered and bound to Marcus to catch the next problem early.", chip: "RPM bound", url: "/his/admin/devices" },
        ],
      },
      {
        act: "Act VIII · The Leadership View",
        tag: "Act VIII",
        title: "What the Organization Sees",
        lines: ["One averted admission, and the governance to scale it safely."],
        scenes: [
          { n: 23, title: "Operational dashboard", beat: "Leadership sees chairs, staff, inventory and the billing queue at a glance.", url: "/his/today" },
          { n: 24, title: "Chair utilization", beat: "The chair board shows floor-wide occupancy and throughput.", url: "/pdms/chairs" },
          { n: 25, title: "HIPAA safeguards", beat: "A live HIPAA Security-Rule safeguard registry; every PHI access is an audited FHIR AuditEvent.", chip: "HIPAA", url: "/admin/hipaa" },
          { n: 26, title: "GDPR data-subject rights", beat: "Consent, Records of Processing, and Art. 15/17 export & erasure are first-class.", chip: "GDPR", url: "/admin/data-protection/data-subject-rights" },
          { n: 27, title: "Identity & access", beat: "Role/permission catalog drives the per-screen access gates across every app.", chip: "RBAC", url: "/admin/identity" },
        ],
      },
    ];

    for (const a of acts) {
      currentAct = a.act;
      await showCard(page, { tag: a.tag, title: a.title, lines: a.lines });
      for (const s of a.scenes) await scene(page, s);
    }

    currentAct = "Outcome";
    await showCard(page, {
      tag: "Outcome",
      title: "Hospitalization Avoided.",
      lines: [
        "A missed session became a caught crisis — not an admission.",
        "<span style='color:#cbd5e1'>Earlier outreach · real-time telemetry · instant escalation · clean interoperability · clean revenue</span>",
        "<b style='color:#34d399'>One platform. One patient journey. Every stakeholder served.</b>",
      ],
    });

    const ok = results.filter((r) => r.ok).length;
    const mins = ((Date.now() - started) / 60_000).toFixed(1);
    console.log(`\n=== scenario film: ${ok}/${results.length} scenes rendered, ${mins} min recorded ===`);
    expect(ok, "most scenes should render real content").toBeGreaterThanOrEqual(Math.ceil(results.length / 2));
  });
});
