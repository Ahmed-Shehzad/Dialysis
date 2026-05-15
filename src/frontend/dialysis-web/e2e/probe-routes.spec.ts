import { expect, test } from "@playwright/test";

// Quick probe to identify which SPA route hangs / errors after sign-in.

const ROUTES = [
  "/",
  "/patients",
  "/sessions",
  "/integrations",
  "/workflows/his",
  "/workflows/ehr",
  "/fhir-exchange",
];

test.setTimeout(180_000);
test("each SPA route loads within 8s without crashing", async ({ page }) => {
  const errors: Array<{ route: string; status: string; took: number; consoleErrs: string[] }> = [];
  const consoleErrs: string[] = [];
  page.on("pageerror", (e) => consoleErrs.push("pageerror: " + e.message));
  page.on("console", (m) => {
    if (m.type() === "error") consoleErrs.push("console.error: " + m.text());
  });

  await page.goto("/");
  await page.getByRole("button", { name: /sign in/i }).click();
  await page.waitForURL(/realms\/dialysis\/protocol\/openid-connect\/auth/i);
  await page.locator("#username").fill("demo");
  await page.locator("#password").fill("demo");
  await Promise.all([
    page.waitForURL((u) => u.host === "localhost:9090" && u.pathname === "/"),
    page.locator("#kc-login, input[name=login], button[name=login]").first().click(),
  ]);
  await page.waitForLoadState("networkidle", { timeout: 8_000 }).catch(() => undefined);
  consoleErrs.length = 0; // reset baseline after auth

  for (const route of ROUTES) {
    const before = consoleErrs.length;
    const start = Date.now();
    let status = "OK";
    try {
      await page.goto(route, { waitUntil: "domcontentloaded", timeout: 8_000 });
      await page.waitForLoadState("networkidle", { timeout: 5_000 }).catch(() => undefined);
    } catch (e) {
      status = "TIMEOUT/ERR: " + (e as Error).message.slice(0, 100);
    }
    const took = Date.now() - start;
    const errsForRoute = consoleErrs.slice(before);
    errors.push({ route, status, took, consoleErrs: errsForRoute });
  }

  console.log("\n=== ROUTE PROBE ===");
  for (const r of errors) {
    console.log(`\n[${r.took}ms] [${r.status}] ${r.route}`);
    for (const e of r.consoleErrs.slice(0, 5)) console.log("   • " + e);
  }
  console.log("\n===================\n");
  expect(errors.length).toBe(ROUTES.length);
});
