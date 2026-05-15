import { expect, test, type Response } from "@playwright/test";

// These tests exercise the full auth chain end-to-end against the live Aspire stack:
//   browser → gateway(9090) → Vite SPA(5173)
//                          → BFF(5275)   [OIDC code flow + cookie + access token relay]
//                          → Keycloak(8081) [realm "dialysis", user demo/demo]
//                          → module APIs(5288/5111/5112/...)
//
// Specifically guarding against the regression we just fixed: the SPA was receiving an
// access token from `/identity/user` but the apiClient decoder (atob without padding /
// UTF-8) was treating it as malformed, returning null from `decodeJwt`, and so the
// Authorization: Bearer header was never attached. The gateway's "authenticated" policy
// then returned 401 for every /api/his/* and /api/pdms/* call.

const KEYCLOAK_USERNAME = process.env.E2E_KC_USERNAME ?? "demo";
const KEYCLOAK_PASSWORD = process.env.E2E_KC_PASSWORD ?? "demo";

// Routes the dashboard fetches on first paint. If any one of these comes back 401, the
// regression is back.
const GATEWAY_API_PATHS = [
  "/api/his/api/v1.0/data-management/manager-dashboard",
  "/api/his/api/v1.0/data-management/integration/outbox-metadata",
  "/api/pdms/api/v1.0/sessions",
];

// Vite cold-transforms each new page module on first hit, so the OIDC round-trip can
// realistically take >60s on a freshly-started stack. Per-test timeout reflects that.
test.describe.configure({ timeout: 180_000 });

test.beforeAll(async ({ request }) => {
  // Fail-fast skip when the stack isn't up — otherwise every assertion below would
  // produce a confusing "page navigation timeout" instead of the real cause.
  try {
    const r = await request.get("/_gateway", { timeout: 2000 });
    if (!r.ok()) test.skip(true, `gateway /_gateway returned ${r.status()} — is Aspire up?`);
  } catch (err) {
    test.skip(true, `gateway not reachable at baseURL — start Aspire AppHost first. (${(err as Error).message})`);
  }
});

const signIn = async (page: import("@playwright/test").Page) => {
  await page.goto("/");
  // The SPA boots into the LoginPage when /identity/user returns 401. Wait for it.
  await expect(page.getByRole("button", { name: /sign in/i })).toBeVisible({ timeout: 15_000 });
  await page.getByRole("button", { name: /sign in/i }).click();

  // Keycloak login form (realm "dialysis"). Login page selectors are stable across
  // Keycloak versions: input#username, input#password, input/button[name=login].
  await page.waitForURL(/realms\/dialysis\/protocol\/openid-connect\/auth/i, { timeout: 15_000 });
  await page.locator("#username").fill(KEYCLOAK_USERNAME);
  await page.locator("#password").fill(KEYCLOAK_PASSWORD);
  await Promise.all([
    page.waitForURL((url) => url.host === "localhost:9090" && url.pathname === "/", { timeout: 60_000 }),
    page.locator("#kc-login, input[name=login], button[name=login]").first().click(),
  ]);
};

test("dashboard's gateway-routed API calls all return 200 after sign-in", async ({ page }) => {
  // Full happy-path lockdown for the dashboard: every API the post-login dashboard fires
  // must return 200. A 401 here means the auth regression is back (decodeJwt / Bearer);
  // a 403 means the role→permission map drifted; a 500 means migrations or seeding broke.
  const apiResponses = new Map<string, Response>();

  page.on("response", (res) => {
    const u = new URL(res.url());
    if (u.host === "localhost:9090" && u.pathname.startsWith("/api/")) {
      apiResponses.set(u.pathname, res);
    }
  });

  await signIn(page);

  await page.waitForResponse(
    (r) => {
      const u = new URL(r.url());
      return u.host === "localhost:9090" && GATEWAY_API_PATHS.includes(u.pathname);
    },
    { timeout: 20_000 },
  );
  await page.waitForLoadState("networkidle", { timeout: 10_000 }).catch(() => undefined);

  for (const path of GATEWAY_API_PATHS) {
    const res = apiResponses.get(path);
    expect(res, `Dashboard never called ${path}`).toBeDefined();
    const status = res!.status();
    expect(status, `${path} returned ${status}. ` +
      `401 → auth regression (decodeJwt / Bearer); 403 → permission map; 500 → migrations/data.`).toBe(200);
  }
});

test("apiClient attaches Authorization: Bearer on every gateway-routed call after sign-in", async ({ page }) => {
  const missingBearer: string[] = [];

  page.on("request", (req) => {
    const u = new URL(req.url());
    if (u.host !== "localhost:9090" || !u.pathname.startsWith("/api/")) return;
    const auth = req.headers()["authorization"];
    if (!auth?.toLowerCase().startsWith("bearer ")) {
      missingBearer.push(u.pathname);
    }
  });

  await signIn(page);
  await page.waitForLoadState("networkidle", { timeout: 10_000 }).catch(() => undefined);

  expect(
    missingBearer,
    "These gateway-routed requests went out without a Bearer token — " +
    "the apiClient interceptor or the decodeJwt path is broken: " + missingBearer.join(", "),
  ).toEqual([]);
});

test("BFF /identity/user returns a non-empty accessToken claim", async ({ page }) => {
  // Use page.evaluate so the BFF session cookie set during OIDC is automatically sent.
  // A standalone APIRequestContext doesn't share the browser's cookie jar.
  await signIn(page);

  const body = await page.evaluate(async () => {
    const r = await fetch("/identity/user", { credentials: "include" });
    return { status: r.status, json: r.ok ? await r.json() : null };
  });

  expect(body.status, "Expected 200 from /identity/user after sign-in").toBe(200);
  const data = body.json as { accessToken?: string } | null;
  expect(typeof data?.accessToken, "/identity/user.accessToken missing — BFF SaveTokens not persisting").toBe("string");
  const token = data!.accessToken!;
  expect(token.length).toBeGreaterThan(100);
  expect(token.split(".")).toHaveLength(3);
});
