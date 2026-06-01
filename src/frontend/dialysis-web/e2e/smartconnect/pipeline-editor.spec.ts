import { expect, request, test, type Page } from "@playwright/test";

// Drives the visual pipeline editor (slice G2 interactive) end-to-end against the live Aspire
// stack. Covers:
//  - Creating a flow via API.
//  - Navigating to /integrations/editor/{flowId} from the Flows tab.
//  - Clicking the "+ Add filter" placeholder, switching the new node's kind to verify-hl7,
//    and saving.
//  - Re-fetching the flow and asserting routeFilters now contains a verify-hl7 entry.
test.describe.configure({ timeout: 180_000 });

const KEYCLOAK_USERNAME = process.env.E2E_KC_USERNAME ?? "demo";
const KEYCLOAK_PASSWORD = process.env.E2E_KC_PASSWORD ?? "demo";

const signIn = async (page: Page) => {
  await page.goto("/");
  await expect(page.getByRole("button", { name: /sign in/i })).toBeVisible({ timeout: 15_000 });
  await page.getByRole("button", { name: /sign in/i }).click();

  await page.waitForURL(/realms\/dialysis\/protocol\/openid-connect\/auth/i, { timeout: 15_000 });
  await page.locator("#username").fill(KEYCLOAK_USERNAME);
  await page.locator("#password").fill(KEYCLOAK_PASSWORD);
  await Promise.all([
    page.waitForURL((url) => url.host === "localhost:9090" && url.pathname === "/", {
      timeout: 60_000,
    }),
    page.locator("#kc-login, input[name=login], button[name=login]").first().click(),
  ]);
};

test("add verify-hl7 filter via the visual editor and save", async ({ page }) => {
  await signIn(page);

  // Create a base channel via the dialog so the editor has a flow id to bind to.
  await page.goto("/admin/smartconnect");
  await expect(page.getByRole("button", { name: /\+ New channel/i })).toBeVisible({
    timeout: 30_000,
  });
  await page.getByRole("button", { name: /\+ New channel/i }).click();
  const channelName = `Playwright Editor ${Date.now()}`;
  await page.getByPlaceholder(/HL7 v2 TCP Listener/i).fill(channelName);
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/smartconnect/v1/admin/flows") && r.request().method() === "POST",
    ),
    page.getByRole("button", { name: /^Create channel$/i }).click(),
  ]);
  await expect(page.getByText(channelName, { exact: true })).toBeVisible({ timeout: 15_000 });

  // Navigate to the editor via the new Edit link on the row.
  await page.locator(`tr:has-text("${channelName}")`).getByRole("link", { name: "Edit" }).click();
  await expect(page.getByRole("heading", { name: /Channel editor/i })).toBeVisible({
    timeout: 15_000,
  });

  // Click the "+ Add filter" placeholder node. xyflow renders nodes as divs containing the
  // label; targeting by visible text is the most robust selector across xyflow versions.
  await page.getByText("+ Add filter", { exact: true }).click();

  // The drawer opens to the new slot. Change its kind to verify-hl7.
  const kindSelect = page.locator("aside select").first();
  await expect(kindSelect).toBeVisible({ timeout: 5_000 });
  await kindSelect.selectOption("verify-hl7");

  // Save.
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/smartconnect/v1/admin/flows/") && r.request().method() === "PUT",
    ),
    page.getByRole("button", { name: /^Save channel$/ }).click(),
  ]);
  await expect(page.getByText(/Channel definition saved\./i)).toBeVisible({ timeout: 10_000 });

  // Re-fetch via the API and confirm the persisted pipeline carries the new filter.
  const apiRequest = await request.newContext({ baseURL: "http://localhost:9090" });
  // The page already has the session cookie; reuse it.
  const cookies = await page.context().cookies();
  apiRequest.dispose();
  const apiViaPage = await page.evaluate(async (name: string) => {
    const list = (await fetch("/api/smartconnect/smartconnect/v1/admin/flows").then((r) =>
      r.json(),
    )) as { id: string; name: string; pipeline: { routeFilters: { kind: string }[] } }[];
    return list.find((f) => f.name === name) ?? null;
  }, channelName);
  expect(apiViaPage, "Created flow must be returned by the admin list endpoint").not.toBeNull();
  const kinds = apiViaPage!.pipeline.routeFilters.map((f) => f.kind);
  expect(kinds).toContain("verify-hl7");

  // Cookies are sent by the browser automatically; the manual request was just a guard.
  void cookies;
});
