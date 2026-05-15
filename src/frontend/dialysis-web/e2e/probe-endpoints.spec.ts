import { expect, test } from "@playwright/test";

// Diagnostic harness: sign in, grab the access token, then probe every endpoint the
// dashboard depends on. Prints status + body[:600] for each so we can pinpoint which
// module-layer errors need fixing.

const ENDPOINTS = [
  "/api/his/api/v1.0/data-management/manager-dashboard",
  "/api/his/api/v1.0/data-management/integration/outbox-metadata?take=25",
  "/api/pdms/api/v1.0/sessions?activeOnly=true&take=100",
  "/api/ehr/api/v1.0/patients?take=25",
  "/api/smartconnect/smartconnect/v1/admin/flows",
  "/api/smartconnect/smartconnect/v1/admin/messages?take=25",
  "/api/his/api/v1.0/reference-architecture/catalog",
  "/api/hie/api/v1.0/fhir/Patient/$match?family=Khan",
];

test.setTimeout(300_000);
test("probe every dashboard-dependent endpoint and report status", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("button", { name: /sign in/i }).click();
  await page.waitForURL(/realms\/dialysis\/protocol\/openid-connect\/auth/i);
  await page.locator("#username").fill("demo");
  await page.locator("#password").fill("demo");
  await Promise.all([
    page.waitForURL((u) => u.host === "localhost:9090" && u.pathname === "/"),
    page.locator("#kc-login, input[name=login], button[name=login]").first().click(),
  ]);

  // Grab the Bearer the BFF gave us, then probe every endpoint with it attached.
  const tokenResp = await page.evaluate(async () => {
    const r = await fetch("/identity/user", { credentials: "include" });
    return r.ok ? (await r.json()) as { accessToken?: string } : null;
  });
  const accessToken = tokenResp?.accessToken;
  if (!accessToken) throw new Error("BFF /identity/user returned no accessToken");

  const probe = await page.evaluate(async ({ urls, token }) => {
    const fetchWithTimeout = async (url: string): Promise<{ status: number; bodyHead: string }> => {
      const ctrl = new AbortController();
      const tid = setTimeout(() => ctrl.abort(), 30_000);
      try {
        const r = await fetch(url, {
          credentials: "include",
          headers: { Accept: "application/json", Authorization: "Bearer " + token },
          signal: ctrl.signal,
        });
        const t = await r.text();
        return { status: r.status, bodyHead: t.slice(0, 400) };
      } catch (err) {
        return { status: 0, bodyHead: "ABORT/ERR: " + String(err).slice(0, 200) };
      } finally {
        clearTimeout(tid);
      }
    };
    const out: Array<{ url: string; status: number; bodyHead: string }> = [];
    for (const u of urls) {
      const r = await fetchWithTimeout(u);
      out.push({ url: u, ...r });
    }
    return out;
  }, { urls: ENDPOINTS, token: accessToken });

  console.log("\n\n=== ENDPOINT PROBE RESULTS ===");
  for (const r of probe) {
    console.log("\n[" + r.status + "] " + r.url);
    console.log(r.bodyHead);
  }
  console.log("\n==============================\n");

  expect(probe.length).toBe(ENDPOINTS.length);
});
