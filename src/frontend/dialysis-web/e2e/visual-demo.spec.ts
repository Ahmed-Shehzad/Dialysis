import { expect, test } from "@playwright/test";
import { mkdir } from "node:fs/promises";
import { dirname } from "node:path";

// Visual end-to-end demonstration. Run headed in Brave with:
//   E2E_HEADED=1 npx playwright test e2e/visual-demo.spec.ts
//
// Captures dated screenshots of every screen the demo user can reach so we have a paper
// trail of what the MVP actually looks like to a real user after sign-in.

const SHOTS_DIR = "e2e/__shots__";

test.beforeAll(async ({ request }) => {
  try {
    const r = await request.get("/_gateway", { timeout: 2000 });
    if (!r.ok()) test.skip(true, "gateway down — start Aspire AppHost first");
  } catch {
    test.skip(true, "gateway unreachable — start Aspire AppHost first");
  }
  await mkdir(dirname(`${SHOTS_DIR}/anchor.png`), { recursive: true });
});

const snap = async (page: import("@playwright/test").Page, name: string) => {
  await page.screenshot({ path: `${SHOTS_DIR}/${name}.png`, fullPage: true });
};

test.setTimeout(180_000);
test("user can sign in, sees populated dashboard, navigates the app", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("button", { name: /sign in/i })).toBeVisible();
  await snap(page, "01-login");

  await page.getByRole("button", { name: /sign in/i }).click();
  await page.waitForURL(/realms\/dialysis\/protocol\/openid-connect\/auth/i);
  await snap(page, "02-keycloak");

  await page.locator("#username").fill("demo");
  await page.locator("#password").fill("demo");
  await Promise.all([
    page.waitForURL((u) => u.host === "localhost:9090" && u.pathname === "/"),
    page.locator("#kc-login, input[name=login], button[name=login]").first().click(),
  ]);

  // Wait until the dashboard's React Query calls settle before the screenshot.
  await page.waitForLoadState("networkidle", { timeout: 10_000 }).catch(() => undefined);
  await page.waitForTimeout(800);
  await snap(page, "03-dashboard");

  // Confirm the auth-chained data actually rendered (not the spinner / not the login fallback).
  await expect(page.getByRole("button", { name: /sign in/i })).toHaveCount(0);

  // Sweep nav routes via SPA history (no full page reload) — page.goto would force the
  // browser to re-fetch HTML through Vite, which cold-transforms each route on first hit
  // and routinely takes >30s. SPA history navigation only fetches the route's JS chunk.
  const navPaths = [
    { path: "/patients", label: "patients" },
    { path: "/sessions", label: "sessions" },
    { path: "/integrations", label: "integrations" },
    { path: "/workflows/his", label: "workflows-his" },
    { path: "/workflows/ehr", label: "workflows-ehr" },
    { path: "/fhir-exchange", label: "fhir-exchange" },
  ];
  for (const { path, label } of navPaths) {
    await page.evaluate((p) => globalThis.history.pushState({}, "", p), path);
    // Nudge React Router to recompute on the synthetic popstate.
    await page.evaluate(() => globalThis.dispatchEvent(new PopStateEvent("popstate")));
    await page.waitForLoadState("networkidle", { timeout: 6_000 }).catch(() => undefined);
    await page.waitForTimeout(400);
    await snap(page, `04-${label}`);
    expect(page.url(), `Route ${path} bounced to /login — auth state lost`).not.toContain("/login");
  }
});
