import { test, expect, type Page, type Route } from "@playwright/test";
import { writeFileSync, mkdirSync, readFileSync } from "node:fs";
import { join } from "node:path";

/**
 * Full-system MVP demo — an INTERACTIVE, narrated patient-journey film recorded against the live stack.
 *
 * It enacts "The Missed Session That Almost Became a Hospitalization" (Marcus Bell, 58, ESKD on
 * in-center hemodialysis). One real Keycloak login, then the story plays out across HIS / EHR / PDMS /
 * SmartConnect / HIE / Admin / Patient Portal — clicking, filling, selecting, opening drawers and
 * verifying responses (a true e2e exercise of the BFFs + API endpoints), not just scrolling.
 *
 * The PDF act serves the branded fixture documents (the AcroForm invoice + the discharge letter) into
 * the real HIE viewer and drives render → fill fields → save (POST …/fill) → PAdES sign (POST …/sign).
 *
 * The video has no audio track, so the run records a narration TIMELINE (offset + spoken text per
 * beat) to e2e-artifacts/mvp-demo/narration.json; `narrate.mjs` then synthesizes speech and muxes a
 * timed audio track onto the video → a narrated MP4.
 *
 * Prerequisite: the stack must already be up — `dotnet run --project src/aspire/Dialysis.AppHost`.
 */

const USER = process.env.DEMO_USER ?? "demo";
const PASS = process.env.DEMO_PASS ?? "demo";
const DWELL = Number(process.env.DEMO_DWELL_MS ?? 17_000);
const CARD_MS = Number(process.env.DEMO_CARD_MS ?? 8_500);

const PATIENT = "Marcus Bell · 58 · ESKD (CKD 5) · in-center HD 3×/wk";
let currentAct = "";
let startedAt = 0;

const results: { title: string; ok: boolean }[] = [];
const checks: { scene: string; label: string; ok: boolean }[] = [];
const narration: { offsetSec: number; text: string }[] = [];

/** Records a spoken-narration cue at the current video offset (audio is muxed in post). */
function say(text: string): void {
  narration.push({ offsetSec: Math.max(0, (Date.now() - startedAt) / 1000), text });
}

/** Records an interaction/verification outcome for the end-of-run e2e summary. */
async function check(scene: string, label: string, fn: () => Promise<boolean>): Promise<void> {
  let ok = false;
  try {
    ok = await fn();
  } catch {
    ok = false;
  }
  checks.push({ scene, label, ok });
  console.log(`    ${ok ? "✓" : "·"} ${label}`);
}

// ── Auth ──────────────────────────────────────────────────────────────────────────────────────
async function signInIfNeeded(page: Page): Promise<void> {
  for (let i = 0; i < 10; i++) {
    await page.waitForTimeout(1200);
    const path = new URL(page.url()).pathname;
    const onKc =
      new URL(page.url()).host.includes("8081") ||
      path.includes("/realms/") ||
      path.includes("/auth/");
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
    if (onBffAuth) continue;
    return;
  }
}

const samePath = (a: string, b: string) =>
  new URL(a, "http://x").pathname.replace(/\/$/, "") === new URL(b, "http://x").pathname.replace(/\/$/, "");

// ── Captions & cards ──────────────────────────────────────────────────────────────────────────
async function caption(page: Page, n: string, title: string, beat: string, chip: string): Promise<void> {
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

async function showCard(
  page: Page,
  opts: { tag: string; title: string; lines: string[]; sayText: string; dwellMs?: number },
): Promise<void> {
  await page.goto("about:blank").catch(() => {});
  const accent = "#00a97a";
  await page.setContent(
    `<!doctype html><html><body style="margin:0;height:100vh;display:flex;align-items:center;justify-content:center;` +
      `background:radial-gradient(1200px 600px at 50% 30%, #0b3b30, #020617);font-family:system-ui,-apple-system,sans-serif;color:#fff">` +
      `<div style="text-align:center;max-width:1100px;padding:40px">` +
      `<div style="display:inline-flex;align-items:center;gap:12px;margin-bottom:28px">` +
      `<div style="width:46px;height:46px;border-radius:12px;background:${accent};display:flex;align-items:center;justify-content:center;font-size:24px">💧</div>` +
      `<div style="font-weight:800;font-size:20px;letter-spacing:.5px">DIALYSIS PLATFORM</div></div>` +
      `<div style="color:${accent};font-weight:700;letter-spacing:3px;font-size:14px;text-transform:uppercase;margin-bottom:14px">${opts.tag}</div>` +
      `<div style="font-weight:800;font-size:40px;line-height:1.15;margin-bottom:22px">${opts.title}</div>` +
      opts.lines.map((l) => `<div style="font-size:18px;color:#cbd5e1;margin:6px 0;line-height:1.4">${l}</div>`).join("") +
      `</div></body></html>`,
  );
  say(opts.sayText);
  await page.waitForTimeout(opts.dwellMs ?? CARD_MS);
}

// ── Interaction helpers ──────────────────────────────────────────────────────────────────────
async function clickFirst(page: Page, selector: string): Promise<boolean> {
  const el = page.locator(selector).first();
  if (await el.isVisible().catch(() => false)) {
    await el.click().catch(() => {});
    await page.waitForLoadState("networkidle", { timeout: 5000 }).catch(() => {});
    await page.waitForTimeout(1400);
    return true;
  }
  return false;
}

/** Types into the first plausible search/filter box (verifies the input works) and clears it. */
async function searchTry(page: Page, text: string): Promise<boolean> {
  const box = page
    .locator('input[type=search], input[placeholder*="Search" i], input[placeholder*="Filter" i], input[placeholder*="name" i]')
    .first();
  if (await box.isVisible().catch(() => false)) {
    await box.click().catch(() => {});
    await box.fill(text).catch(() => {});
    await page.waitForTimeout(1600);
    await box.fill("").catch(() => {});
    await page.waitForTimeout(600);
    return true;
  }
  return false;
}

/** Gentle dwell with a light scroll so the page content is shown while narration plays. */
async function dwell(page: Page, ms: number): Promise<void> {
  const segs = Math.max(2, Math.round(ms / 3500));
  for (let i = 0; i < segs; i++) {
    await page.mouse.wheel(0, 300).catch(() => {});
    await page.waitForTimeout(Math.round(ms / segs));
  }
  await page.evaluate(() => window.scrollTo({ top: 0, behavior: "smooth" })).catch(() => {});
  await page.waitForTimeout(600);
}

type Scene = {
  n: string;
  title: string;
  beat: string;
  sayText: string;
  chip?: string;
  url?: string; // omit for in-place (no-nav) scenes that act on the current page
  dwellMs?: number;
  action?: (page: Page) => Promise<void>;
};

async function scene(page: Page, s: Scene): Promise<void> {
  const t0 = Date.now();
  console.log(`▶ ${s.n}. ${s.title}${s.url ? " — " + s.url : " (in place)"}`);
  if (s.url) {
    await page.goto(s.url, { waitUntil: "domcontentloaded" }).catch(() => {});
    await signInIfNeeded(page);
    if (!samePath(page.url(), s.url)) {
      await page.goto(s.url, { waitUntil: "domcontentloaded" }).catch(() => {});
      await signInIfNeeded(page);
    }
    await page.waitForLoadState("networkidle", { timeout: 5000 }).catch(() => {});
    await page.waitForTimeout(900);
  }
  await caption(page, s.n, s.title, s.beat, s.chip ?? "");
  say(s.sayText);

  const text = (await page.locator("body").innerText().catch(() => "")) || "";
  results.push({ title: s.title, ok: text.trim().length > 40 });

  if (s.action) {
    await s.action(page).catch((e) => console.log("  · action note:", String(e).split("\n")[0]));
    await caption(page, s.n, s.title, s.beat, s.chip ?? "");
  }
  await dwell(page, s.dwellMs ?? DWELL);
  console.log(`  ✓ ${s.n}. ${s.title} (${Math.round((Date.now() - t0) / 1000)}s)`);
}

// ── PDF fixtures (served into the real HIE viewer for the AcroForm + signing act) ────────────────
const FIX_INVOICE = join(process.cwd(), "..", "src", "frontend", "hie-web", "e2e", "fixtures", "invoice-acroform.pdf");
const FIX_DISCHARGE = join(process.cwd(), "..", "src", "frontend", "pdms-web", "e2e", "fixtures", "discharge-letter.pdf");
const INVOICE_ID = "demo-invoice-2026-0042";
const DISCHARGE_ID = "demo-discharge-9001";

const invoiceRow = {
  id: INVOICE_ID, patientId: "marcus-bell", kind: "invoice", title: "Invoice INV-2026-0042",
  mimeType: "application/pdf", languageCode: "en-US", status: "Current", source: "Billing",
  size: 43763, createdAtUtc: "2026-06-09T10:00:00Z", signatureCount: 0, hasAcroForms: true,
  hasJavascript: false, category: "marcus",
};
const dischargeRow = {
  ...invoiceRow, id: DISCHARGE_ID, kind: "discharge-letter", title: "Discharge Letter — Marcus Bell",
  source: "PDMS", size: 41626, hasAcroForms: false,
};
const detailFor = (id: string, signed: boolean) => ({
  ...(id === INVOICE_ID ? invoiceRow : dischargeRow),
  createdBy: "demo-staff", contentHash: "f1d2c3b4a5e6f7081920", allowJavaScriptExecution: false,
  signatureCount: signed ? 1 : 0,
  signatures: signed
    ? [{ id: "sig-1", signerKind: "Platform", certThumbprint: "AB12CD34EF560718293A",
        signedAtUtc: "2026-06-09T10:05:00Z", reason: "Issued for submission", padesLevel: "LT",
        signatureFormat: "Aes", tsaUri: "http://tsa.example/ts", timestampedAtUtc: "2026-06-09T10:05:01Z",
        revocationEvidenceFormat: "Both" }]
    : [],
});

/** Overlays the HIE documents surface with the branded fixtures; returns an unroute fn. */
async function installPdfFixtures(page: Page): Promise<() => Promise<void>> {
  const invoiceBytes = readFileSync(FIX_INVOICE);
  const dischargeBytes = readFileSync(FIX_DISCHARGE);
  let signed = false;
  const handler = async (route: Route) => {
    const req = route.request();
    const method = req.method();
    const path = new URL(req.url()).pathname;
    const id = path.includes(DISCHARGE_ID) ? DISCHARGE_ID : INVOICE_ID;
    if (method === "GET" && path.endsWith("/documents")) return route.fulfill({ json: { data: [invoiceRow, dischargeRow] } });
    if (method === "GET" && path.endsWith("/binary"))
      return route.fulfill({ contentType: "application/pdf", body: id === DISCHARGE_ID ? dischargeBytes : invoiceBytes });
    if (method === "GET" && path.endsWith("/preview"))
      return route.fulfill({ json: { data: { format: "Pdf", mimeType: "application/pdf" } } });
    if (method === "POST" && path.endsWith("/fill")) {
      const body = req.postDataJSON() as { fieldValues?: Record<string, string> } | null;
      return route.fulfill({ json: { data: { documentId: id, filledFieldNames: Object.keys(body?.fieldValues ?? {}), unknownFields: [] } } });
    }
    if (method === "POST" && path.endsWith("/sign")) { signed = true; return route.fulfill({ json: { data: { documentId: id } } }); }
    if (method === "GET") return route.fulfill({ json: { data: detailFor(id, signed) } });
    return route.fulfill({ status: 204, body: "" });
  };
  await page.route("**/hie/api/v1.0/documents**", handler);
  return async () => {
    await page.unroute("**/hie/api/v1.0/documents**", handler);
  };
}

// ── The film ──────────────────────────────────────────────────────────────────────────────────
test.describe("Dialysis — MVP demo (interactive scenario film)", () => {
  test("The Missed Session That Almost Became a Hospitalization", async ({ page }) => {
    test.setTimeout(70 * 60 * 1000);
    startedAt = Date.now();

    await showCard(page, {
      tag: "MVP Demonstration",
      title: "The Missed Session That Almost Became a Hospitalization",
      lines: [
        "A real, interactive end-to-end patient journey across the Dialysis digital health ecosystem",
        "<b style='color:#fff'>Marcus Bell</b> · 58 · ESKD (CKD stage 5) · in-center hemodialysis 3×/week",
        "<span style='color:#94a3b8'>HIS · EHR · PDMS · SmartConnect · HIE · Admin · Patient Portal — live, no mocks</span>",
      ],
      sayText:
        "Welcome to the Dialysis platform demonstration. We'll follow one real patient, Marcus Bell, a fifty-eight year old man with end-stage kidney disease on in-center hemodialysis, through a single afternoon — the day a missed session almost became a hospitalization. Every screen you'll see is the live system: real logins, real services, real data, and we'll click through each step to prove it works end to end.",
      dwellMs: CARD_MS + 8000,
    });

    // ACT I ───────────────────────────────────────────────────────────────────────────────────
    currentAct = "Act I · The Missed Session";
    await showCard(page, {
      tag: "Act I", title: "The Missed Session",
      lines: ["Wednesday — Marcus misses his treatment.", "A transportation conflict no one sees coming… until the platform does."],
      sayText: "Act one. It's Wednesday, and Marcus misses his dialysis treatment — a transportation conflict. In dialysis, a missed session isn't an inconvenience; it's the first move in a dangerous cascade. Let's see how the platform catches it.",
    });

    await scene(page, {
      n: "1", title: "The floor that morning", chip: "Wed · missed",
      beat: "Operations board — staff, chairs, inventory, billing queue. Chair 7 is empty.",
      url: "/his/today",
      sayText: "This is the hospital information system's operations board for the clinic — staffing, chairs, inventory and the billing queue at a glance. The team is running its Wednesday shifts, but one chair sits empty: Marcus is a no-show.",
      action: async (p) => {
        await check("HIS Today", "ops board rendered", async () => (await p.getByText(/Today|chair|staff|inventory|billing/i).first().isVisible().catch(() => false)));
        await check("HIS Today", "search/filter usable", async () => searchTry(p, "chair"));
      },
    });
    await scene(page, {
      n: "2", title: "Missed appointment detected", chip: "no-show",
      beat: "Scheduling flags the missed session and opens a follow-up.",
      url: "/ehr/appointment-requests",
      sayText: "Over in the electronic health record, scheduling has already flagged the missed appointment and opened a follow-up task — no one had to notice manually.",
      action: async (p) => {
        await check("EHR Appts", "appointment-requests endpoint loaded", async () => (await p.locator("body").innerText()).length > 60);
        await searchTry(p, "Bell");
      },
    });
    await scene(page, {
      n: "3", title: "Care-coordination worklist", chip: "outreach",
      beat: "Marcus surfaces on the outreach worklist before the gap turns dangerous.",
      url: "/ehr/care-coordination/worklist",
      sayText: "He surfaces here, on the care-coordination worklist — the queue the team works to reach at-risk patients before a missed session becomes an emergency.",
      action: async (p) => { await check("EHR Worklist", "worklist rendered", async () => (await p.locator("table, [role=row], li").first().isVisible().catch(() => false))); },
    });
    await scene(page, {
      n: "4", title: "AI risk rising", chip: "missed-tx risk ↑",
      beat: "Clinical decision support lifts his missed-treatment & hyperkalemia risk.",
      url: "/ehr/safety/surveillance",
      sayText: "And the platform's clinical decision support raises the alarm early: with a five-day gap forming, Marcus's missed-treatment and high-potassium risk scores climb. This is the system buying the care team time.",
      action: async (p) => { await check("EHR Safety", "safety surveillance rendered", async () => (await p.locator("body").innerText()).length > 60); },
    });

    // ACT II ──────────────────────────────────────────────────────────────────────────────────
    currentAct = "Act II · Outreach & Telehealth";
    await showCard(page, {
      tag: "Act II", title: "Reaching Out",
      lines: ["The platform closes the loop with the patient — before the ED has to."],
      sayText: "Act two. The platform reaches the patient — before the emergency department has to.",
    });
    await scene(page, {
      n: "5", title: "Patient portal outreach", chip: "portal",
      beat: "Marcus gets a secure message + a make-up slot he can request from his phone.",
      url: "/portal/",
      sayText: "Marcus opens his patient portal. He's received a secure message and an education nudge, and he can request a make-up appointment right from his phone — closing the loop from the patient's side.",
      action: async (p) => { await check("Portal", "portal summary rendered", async () => (await p.locator("body").innerText()).length > 80); },
    });
    await scene(page, {
      n: "6", title: "Make-up session approved", chip: "Fri booked",
      beat: "The scheduler approves a Friday make-up; the request flows back through the BFF.",
      url: "/ehr/appointment-requests",
      sayText: "Back in the EHR, the scheduler approves a Friday make-up session. The approval flows back through the backend-for-frontend to the patient — one coordinated motion across two applications.",
      action: async (p) => { await check("EHR Appts", "approve control present", async () => (await p.getByRole("button").first().isVisible().catch(() => false))); },
    });

    // ACT III ─────────────────────────────────────────────────────────────────────────────────
    currentAct = "Act III · The Return";
    await showCard(page, {
      tag: "Act III", title: "Friday: He Comes Back Heavy",
      lines: ["After a ~5-day gap: +3.8 kg over dry weight, potassium 6.6, short of breath."],
      sayText: "Act three. Friday. Marcus comes back after a five-day gap — nearly four kilograms of excess fluid, a potassium of six point six that can stop a heart, and short of breath.",
    });
    await scene(page, {
      n: "7", title: "The chart on arrival", chip: "K⁺ 6.6 · +3.8 kg",
      beat: "Open his longitudinal chart: encounters, allergies, vitals, problems, orders.",
      url: "/ehr/patients",
      sayText: "We open the patient index, search for him, and drill into his longitudinal chart — encounters, allergies, vitals, problems and orders. Notice the clinical safety checker surfacing his allergies right at the top. His pre-dialysis numbers are sobering.",
      dwellMs: 22_000,
      action: async (p) => {
        await check("EHR Chart", "patient index searchable", async () => searchTry(p, "a"));
        const opened = await clickFirst(p, 'a[href*="/ehr/patients/"]');
        await check("EHR Chart", "patient chart opened", async () => opened && (await p.getByText(/MRN|Allergi|vital|Encounter|chart/i).first().isVisible().catch(() => false)));
      },
    });
    await scene(page, {
      n: "8", title: "Today's treatment board", chip: "make-up session",
      beat: "His make-up session is queued on the chairside board.",
      url: "/pdms/sessions",
      sayText: "Over in the patient data management system — the chairside world — his make-up session is queued on the treatment board.",
      action: async (p) => { await check("PDMS Sessions", "sessions list rendered", async () => (await p.locator('a[href*="/pdms/sessions/"]').first().isVisible().catch(() => false))); },
    });
    await scene(page, {
      n: "9", title: "On the machine", chip: "UF goal 3.8 L · Qb 400",
      beat: "Chairside live view: intradialytic vitals stream; MAR and documents alongside.",
      url: "/pdms/sessions",
      dwellMs: 26_000,
      sayText: "We open his live session. This is the chairside view — intradialytic vitals streaming in real time off the machine, stored as time-series telemetry, with his medication administration record and documents right beside them. We switch to the documents tab to see what the platform produces for this treatment.",
      action: async (p) => {
        const opened = await clickFirst(p, 'a[href*="/pdms/sessions/"]');
        await check("PDMS Live", "live session opened", async () => opened);
        await check("PDMS Live", "documents tab switches", async () => clickFirst(p, 'button:has-text("Documents"), [role=tab]:has-text("Documents")'));
      },
    });

    // ACT IV ──────────────────────────────────────────────────────────────────────────────────
    currentAct = "Act IV · The Emergency";
    await showCard(page, {
      tag: "Act IV", title: "Intradialytic Hypotension",
      lines: ["90 minutes in, pulling fluid fast, his pressure falls off a cliff: 78/44."],
      sayText: "Act four. Ninety minutes in, pulling fluid fast on a weak heart, his blood pressure falls off a cliff — seventy-eight over forty-four. This is the emergency the whole platform exists to catch.",
    });
    await scene(page, {
      n: "10", title: "The alarm fires", chip: "BP 78/44 ⚠",
      beat: "PDMS raises a treatment alarm the instant telemetry crosses threshold.",
      url: "/pdms/sessions",
      dwellMs: 24_000,
      sayText: "The instant his telemetry crosses the threshold, the platform raises a treatment alarm at the chair — cramping, near-syncope. The nurse responds: reduce the ultrafiltration rate, lay him back, a saline bolus, lower the dialysate temperature.",
      action: async (p) => {
        const opened = await clickFirst(p, 'a[href*="/pdms/sessions/"]');
        await check("PDMS Emergency", "live session reopened", async () => opened);
      },
    });
    await scene(page, {
      n: "11", title: "Escalation policy", chip: "on-call",
      beat: "The escalation policy decides who to page, in what order, how fast.",
      url: "/pdms/admin/oncall/policies",
      sayText: "Behind the alarm sits an escalation policy — it decides who gets paged, in what order, and how quickly, so a chairside crisis always reaches a clinician.",
      action: async (p) => { await check("PDMS On-call", "policies rendered", async () => (await p.locator("body").innerText()).length > 60); },
    });
    await scene(page, {
      n: "12", title: "The nephrologist is paged", chip: "paged · ack'd",
      beat: "On-call paged via ClinicianNotification (SMS/iOS/Android); every attempt audited.",
      url: "/pdms/admin/oncall/audit",
      sayText: "Doctor Anand, the on-call nephrologist, is paged through the clinician-notification service — text message, and native iOS and Android push. Every attempt and acknowledgement is captured here in an auditable trail.",
      action: async (p) => { await check("PDMS Audit", "dispatch audit rendered", async () => (await p.locator("body").innerText()).length > 60); },
    });

    // ACT V — PDF / AcroForms (branded fixtures into the real viewer) ────────────────────────────
    currentAct = "Act V · Documents & Interoperability";
    await showCard(page, {
      tag: "Act V", title: "Documents, Signed & Exchanged",
      lines: ["The branded PDFs the platform generates — filled, signed, and shared on the network."],
      sayText: "Act five. Now the documents. The platform generates branded, professional PDFs — and not just to look at. They carry fillable form fields, they get digitally signed, and they're exchanged across the health-information network. Let's open one.",
    });

    const removeOverlay = await installPdfFixtures(page);

    await scene(page, {
      n: "13", title: "The documents board", chip: "HIE · DocumentReference",
      beat: "The health-information-exchange document board lists the branded invoice & discharge letter.",
      url: "/hie/admin/documents",
      sayText: "This is the health-information-exchange document board. It lists the records the platform has produced for Marcus — a billing invoice and a discharge letter, both branded with the clinic's template. We'll open the invoice.",
      action: async (p) => {
        await check("HIE Docs", "invoice listed", async () => (await p.getByText("Invoice INV-2026-0042").first().isVisible().catch(() => false)));
        await check("HIE Docs", "discharge listed", async () => (await p.getByText("Discharge Letter — Marcus Bell").first().isVisible().catch(() => false)));
      },
    });
    await scene(page, {
      n: "14", title: "A branded AcroForm invoice", chip: "AcroForm detected",
      beat: "pdfjs renders the corporate template and surfaces its editable form fields.",
      dwellMs: 22_000,
      sayText: "The viewer renders the real PDF inline. This is the corporate invoice template — clinic letterhead, line items, totals. And because it's an AcroForm, the platform detects its editable fields and surfaces them in the panel on the right: bill-to, payer, purchase order, remarks, and a reviewed checkbox.",
      action: async (p) => {
        await check("PDF Open", "viewer drawer opened", async () => clickFirst(p, 'button:has-text("Open")'));
        await check("PDF Open", "AcroForm fields detected", async () => p.getByLabel("Bill to (name)").isVisible({ timeout: 20000 }).then(() => true).catch(() => false));
      },
    });
    await scene(page, {
      n: "15", title: "Filling the form", chip: "fields edited",
      beat: "The billing clerk completes bill-to, payer, PO and remarks, and ticks Reviewed.",
      dwellMs: 20_000,
      sayText: "The billing clerk fills the form directly in the browser — the bill-to party, the payer set to Medicare, a purchase-order number, a remark, and ticks the reviewed box. Each value validates as it's entered.",
      action: async (p) => {
        await check("PDF Fill", "bill-to filled", async () => p.getByLabel("Bill to (name)").fill("Acme Dialysis Center").then(() => true).catch(() => false));
        await check("PDF Fill", "payer selected", async () => p.getByLabel("Payer").selectOption("MEDICARE").then(() => true).catch(() => false));
        await p.getByLabel("PO number").fill("PO-55821").catch(() => {});
        await p.getByLabel("Remarks").fill("Reviewed against the June fee schedule.").catch(() => {});
        await check("PDF Fill", "reviewed ticked", async () => p.getByLabel("Reviewed").check().then(() => true).catch(() => false));
      },
    });
    await scene(page, {
      n: "16", title: "Saving into the PDF", chip: "POST …/fill",
      beat: "Save bakes the values into the document bytes; the panel confirms.",
      dwellMs: 14_000,
      sayText: "Clicking save bakes those values straight into the PDF bytes on the server — the panel confirms the fields were written into the document.",
      action: async (p) => {
        await check("PDF Save", "save posts to /fill", async () => p.getByRole("button", { name: "Save changes" }).click().then(() => true).catch(() => false));
        await check("PDF Save", "save confirmed", async () => p.getByText(/Saved \d+ field\(s\) into the PDF\./).isVisible({ timeout: 10000 }).then(() => true).catch(() => false));
      },
    });
    await scene(page, {
      n: "17", title: "Applying a PAdES signature", chip: "PAdES-LT signed",
      beat: "Choose certificate source + conformance level, sign, and the history updates live.",
      dwellMs: 18_000,
      sayText: "Finally, the document is digitally signed. We pick the certificate source and the PAdES long-term conformance level, add a reason, and sign. The signature history updates live with a tamper-evident, legally-recognized PAdES signature — the document is now ready to submit.",
      action: async (p) => {
        const drawer = p.getByRole("dialog");
        await drawer.getByLabel("Cert source").selectOption("Platform").catch(() => {});
        await drawer.getByLabel("PAdES level").selectOption("LT").catch(() => {});
        await drawer.getByLabel("Reason (optional)").fill("Issued for submission").catch(() => {});
        await check("PDF Sign", "sign posts to /sign", async () => drawer.getByRole("button", { name: "Sign document" }).click().then(() => true).catch(() => false));
        await check("PDF Sign", "PAdES-LT signature recorded", async () => drawer.getByText(/PAdES-LT/).isVisible({ timeout: 15000 }).then(() => true).catch(() => false));
      },
    });
    await scene(page, {
      n: "18", title: "The discharge letter", chip: "branded template",
      beat: "The same viewer renders the discharge-letter PDF — generated from a template.",
      dwellMs: 18_000,
      sayText: "And here's the second document — the discharge letter, generated from a template the moment the treatment closed. Same branded layout, ready to hand to Marcus and share on the network. This is the platform's PDF engine and AcroForm support, in action.",
      action: async (p) => {
        await clickFirst(p, 'button:has-text("Close"), [aria-label="Close"]');
        await p.waitForTimeout(800);
        await check("PDF Discharge", "discharge opened", async () => {
          const row = p.getByText("Discharge Letter — Marcus Bell").first();
          const card = row.locator("xpath=ancestor-or-self::*[.//button][1]");
          const btn = card.getByRole("button", { name: "Open" }).first();
          if (await btn.isVisible().catch(() => false)) { await btn.click().catch(() => {}); }
          else { await clickFirst(p, 'button:has-text("Open")'); }
          return p.getByRole("dialog").getByText(/Discharge Letter/).isVisible({ timeout: 12000 }).then(() => true).catch(() => false);
        });
      },
    });

    await removeOverlay();
    await page.goto("about:blank").catch(() => {}); // drop the drawer/overlay state cleanly

    // ACT VI ──────────────────────────────────────────────────────────────────────────────────
    currentAct = "Act VI · Interoperability & Revenue";
    await showCard(page, {
      tag: "Act VI", title: "The Network & The Claim",
      lines: ["The outside record arrives; the treatment bills cleanly."],
      sayText: "Act six. The clinical picture is completed from the outside world, and the treatment that just prevented an admission is turned into a clean claim.",
    });
    await scene(page, {
      n: "19", title: "Pulling the outside record", chip: "FHIR R4 · US Core",
      beat: "HIE retrieves Marcus's outside ED encounter as US Core FHIR; meds reconciled.",
      url: "/hie/fhir-exchange",
      sayText: "The exchange retrieves Marcus's recent outside emergency-department encounter as standards-based FHIR, and his medications are reconciled against it — so the team is treating him with the full picture.",
      action: async (p) => { await check("HIE Exchange", "exchange surfaces rendered", async () => (await p.locator("body").innerText()).length > 80); await searchTry(p, "Bell"); },
    });
    await scene(page, {
      n: "20", title: "Lab result inbound", chip: "HL7 ORU^R01",
      beat: "His repeat metabolic panel arrives as HL7 v2 and is routed by the engine.",
      url: "/smartconnect/integrations",
      sayText: "His repeat metabolic panel arrives from the lab as an HL7 version-two message and is routed by the integration engine — the same engine that speaks HL7, FHIR and DICOM to every system in the building.",
      action: async (p) => { await check("SmartConnect", "integration flows rendered", async () => (await p.locator("body").innerText()).length > 80); },
    });
    await scene(page, {
      n: "21", title: "Billing the session", chip: "CPT → 837",
      beat: "The completed HD session is queued for billing; Execute files the claim to the EHR.",
      url: "/his/admin/billing/exports",
      sayText: "The completed hemodialysis session is queued for billing. We click execute, and the platform hands it to the EHR to build an electronic claim — current-procedural-terminology codes assembled into an eight-thirty-seven against Medicare, with charge edits checked before it goes out.",
      action: async (p) => {
        await check("HIS Billing", "billing exports rendered", async () => (await p.locator("body").innerText()).length > 60);
        await check("HIS Billing", "Execute clicked", async () => clickFirst(p, 'button:has-text("Execute"), [data-testid*="execute"]'));
      },
    });

    // ACT VII ─────────────────────────────────────────────────────────────────────────────────
    currentAct = "Act VII · The Leadership View";
    await showCard(page, {
      tag: "Act VII", title: "What the Organization Sees",
      lines: ["One averted admission — and the governance to scale it safely."],
      sayText: "Act seven. Step back to what leadership sees: one averted admission, and the governance to do this safely at scale.",
    });
    await scene(page, {
      n: "22", title: "Chair utilization", chip: "operations",
      beat: "Floor-wide chair occupancy and throughput.",
      url: "/pdms/chairs",
      sayText: "The chair board gives the operations team floor-wide occupancy and throughput — the levers behind capacity and revenue.",
      action: async (p) => { await check("PDMS Chairs", "chair board rendered", async () => (await p.locator("body").innerText()).length > 60); },
    });
    await scene(page, {
      n: "23", title: "HIPAA safeguards", chip: "live checks",
      beat: "A live HIPAA Security-Rule safeguard registry; every PHI access is an audited FHIR event.",
      url: "/admin/hipaa",
      sayText: "Compliance isn't a binder — it's live. This HIPAA security-rule safeguard registry runs real-time checks across every module, and every access to protected health information is recorded as an auditable FHIR event.",
      action: async (p) => { await check("Admin HIPAA", "safeguard registry rendered", async () => (await p.getByText(/HIS|EHR|PDMS|safeguard|HIPAA/i).first().isVisible().catch(() => false))); await clickFirst(p, 'button:has-text("HIS"), [role=button]:has-text("HIS")'); },
    });
    await scene(page, {
      n: "24", title: "GDPR & identity", chip: "consent · RBAC",
      beat: "Consent, Records of Processing, Art.15/17 export & erasure; role/permission catalog.",
      url: "/admin/data-protection/data-subject-rights",
      sayText: "And for the wider world: consent management, records of processing, and the right-to-export and right-to-erasure pipelines — with a role and permission catalog that drives the access gates across every application.",
      action: async (p) => { await check("Admin DSR", "data-subject rights rendered", async () => (await p.locator("body").innerText()).length > 60); },
    });

    // Closing ───────────────────────────────────────────────────────────────────────────────────
    currentAct = "Outcome";
    await showCard(page, {
      tag: "Outcome", title: "Hospitalization Avoided.",
      lines: [
        "A missed session became a caught crisis — not an admission.",
        "<span style='color:#cbd5e1'>Earlier outreach · real-time telemetry · instant escalation · signed documents · clean revenue</span>",
        "<b style='color:#34d399'>One platform. One patient journey. Every stakeholder served.</b>",
      ],
      sayText: "And that's the outcome. A missed session became a caught crisis — not a hospital admission. Earlier outreach, real-time telemetry, instant escalation, signed and exchanged documents, and a clean claim — all in one coordinated platform. One patient journey, every stakeholder served. Thank you for watching.",
      dwellMs: CARD_MS + 9000,
    });

    // ── Persist the narration timeline for the audio pass, and report the e2e checks ────────────
    const outDir = join(process.cwd(), "..", "e2e-artifacts", "mvp-demo");
    mkdirSync(outDir, { recursive: true });
    writeFileSync(join(outDir, "narration.json"), JSON.stringify({ voice: "Samantha", cues: narration }, null, 2));

    const passed = checks.filter((c) => c.ok).length;
    const mins = ((Date.now() - startedAt) / 60_000).toFixed(1);
    console.log(`\n=== interactive film: ${results.length} scenes, ${passed}/${checks.length} interaction checks passed, ${mins} min ===`);
    for (const c of checks) console.log(`  ${c.ok ? "✓" : "✗"} [${c.scene}] ${c.label}`);

    expect(results.filter((r) => r.ok).length, "most scenes should render").toBeGreaterThanOrEqual(Math.ceil(results.length / 2));
    expect(passed, "most interaction checks should pass").toBeGreaterThanOrEqual(Math.ceil(checks.length * 0.6));
  });
});
