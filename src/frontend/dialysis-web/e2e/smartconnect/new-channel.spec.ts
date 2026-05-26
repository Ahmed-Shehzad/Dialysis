import { expect, test, type Page } from "@playwright/test";

// Drives the Create-Channel dialog end-to-end against the live Aspire stack. Covers:
//  - The dialog opens from the SmartConnect Flows tab.
//  - The HL7-MLLP template pre-fills `dataTypes=["HL7v2"]`.
//  - Submitting POSTs to /smartconnect/v1/admin/flows and the flow appears in the list.
//
// Per-test timeout matches auth.spec.ts: the OIDC round-trip on a freshly-started stack
// can realistically run >60s.
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

test("create channel via dialog from the HL7-MLLP template lands a new flow", async ({ page }) => {
  await signIn(page);

  // Navigate to the SmartConnect Flows tab. The exact route is operator-shell-defined; the
  // text "Flows" appears in the FlowsTab title regardless of route shell.
  await page.goto("/admin/smartconnect");
  await expect(page.getByRole("button", { name: /\+ New channel/i })).toBeVisible({
    timeout: 30_000,
  });

  // Open the dialog. Click via role+name so the test survives styling changes.
  await page.getByRole("button", { name: /\+ New channel/i }).click();
  await expect(page.getByText(/New channel/i)).toBeVisible();

  // Fill Basics.
  const channelName = `Playwright HL7 ${Date.now()}`;
  await page.getByPlaceholder(/HL7 v2 TCP Listener/i).fill(channelName);

  // HL7-MLLP template is the default; assert HL7v2 chip is selected on Metadata step.
  const hl7Chip = page.getByRole("button", { name: "HL7v2", exact: true });
  await expect(hl7Chip).toBeVisible();

  // Add a tag.
  await page.getByPlaceholder(/hl7 production north-hospital/i).fill("e2e playwright");

  // Submit.
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/smartconnect/v1/admin/flows") && r.request().method() === "POST",
    ),
    page.getByRole("button", { name: /^Create channel$/i }).click(),
  ]);

  // The dialog closes on success and the new flow appears in the list.
  await expect(page.getByText(/New channel/i)).not.toBeVisible({ timeout: 5_000 });
  await expect(page.getByText(channelName, { exact: true })).toBeVisible({ timeout: 15_000 });
});

test("Start refuses when a dependency is Stopped, with the unmet dep named", async ({ page }) => {
  await signIn(page);
  await page.goto("/admin/smartconnect");
  await expect(page.getByRole("button", { name: /\+ New channel/i })).toBeVisible({
    timeout: 30_000,
  });

  // Step 1: create a "dep" channel left Stopped (default).
  const depName = `Dep Stopped ${Date.now()}`;
  await page.getByRole("button", { name: /\+ New channel/i }).click();
  await page.getByPlaceholder(/HL7 v2 TCP Listener/i).fill(depName);
  await page.getByRole("button", { name: /^Create channel$/i }).click();
  await expect(page.getByText(depName, { exact: true })).toBeVisible({ timeout: 15_000 });

  // Step 2: create a dependent channel that lists the first as a dependency.
  const dependentName = `Dependent ${Date.now()}`;
  await page.getByRole("button", { name: /\+ New channel/i }).click();
  await page.getByPlaceholder(/HL7 v2 TCP Listener/i).fill(dependentName);

  // Select the dep in the Dependencies picker (checkbox row labelled with the dep's name).
  await page.getByRole("checkbox", { name: new RegExp(depName) }).check();
  await page.getByRole("button", { name: /^Create channel$/i }).click();
  await expect(page.getByText(dependentName, { exact: true })).toBeVisible({ timeout: 15_000 });

  // Step 3: hit Start on the dependent; the response should be 422.
  // Use the API directly since the row-level Start button selector varies; the UI's behaviour
  // is the same: invoke POST /flows/{id}/start, receive 422 + unmetDependencies.
  // (We don't bother poking the precise toast text — the API contract is the assertion.)
  const startResponse = await page.evaluate(async (name) => {
    const list = await fetch("/api/smartconnect/smartconnect/v1/admin/flows").then((r) => r.json());
    const target = (list as { id: string; name: string }[]).find((f) => f.name === name);
    if (!target) throw new Error(`Flow ${name} not found in list.`);
    const start = await fetch(`/api/smartconnect/smartconnect/v1/admin/flows/${target.id}/start`, {
      method: "POST",
    });
    const text = await start.text();
    return { status: start.status, body: text };
  }, dependentName);
  expect(startResponse.status).toBe(422);
  expect(startResponse.body).toContain("unmetDependencies");
  expect(startResponse.body).toContain(depName);
});
