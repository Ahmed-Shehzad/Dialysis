import { expect, test, type Page } from "@playwright/test";

// Drives the HL7 Workbench end-to-end against the live Aspire stack. Covers:
//  - Pasting a real HL7 v2.5 ADT^A01 and clicking Parse renders the header + segment names.
//  - Validating against minVersion=2.5 + required segments shows the green ✓ Valid panel.
//
// Skips the dispatch step deliberately — that requires a started HL7v2 channel and is exercised
// indirectly by the dependency-Start spec in new-channel.spec.ts. Keeping this spec narrowly
// scoped on parse + validate keeps it fast on a freshly-started stack.
test.describe.configure({ timeout: 180_000 });

const KEYCLOAK_USERNAME = process.env.E2E_KC_USERNAME ?? "demo";
const KEYCLOAK_PASSWORD = process.env.E2E_KC_PASSWORD ?? "demo";

const SAMPLE_HL7 =
  "MSH|^~\\&|SENDA|FACA|RECB|FACB|20260101010101||ADT^A01|MSGID-WB-1|P|2.5\r" +
  "EVN|A01|20260101010101\r" +
  "PID|1||MRN-12345||DOE^JANE||19800101|F\r" +
  "PV1|1|I|WARD^101^A";

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

const openWorkbench = async (page: Page) => {
  await page.goto("/admin/smartconnect?tab=hl7-workbench");
  await expect(page.getByRole("heading", { name: /HL7 Workbench/i })).toBeVisible({
    timeout: 30_000,
  });
};

test("paste → parse renders the HL7 v2.5 header and segment tree", async ({ page }) => {
  await signIn(page);
  await openWorkbench(page);

  await page.getByPlaceholder(/MSH\|.*ADT\^A01/i).fill(SAMPLE_HL7);

  // The detected-version line should reflect MSH-12.
  await expect(page.getByText(/Detected version \(MSH\.12\):.*2\.5/)).toBeVisible({
    timeout: 5_000,
  });

  // Hitting Parse calls /admin/workbench/parse-hl7 — wait for the network round-trip + UI update.
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/admin/workbench/parse-hl7") && r.request().method() === "POST",
    ),
    page.getByRole("button", { name: /^Parse$/ }).click(),
  ]);

  // The structured parse result panel renders the header fields + a Segments line.
  await expect(page.getByText(/Trigger:/i)).toBeVisible({ timeout: 10_000 });
  await expect(page.getByText(/Version:/i)).toBeVisible();
  await expect(page.getByText(/Segments:.*PID/i)).toBeVisible();
});

test("paste → validate with minVersion 2.5 + required segments passes", async ({ page }) => {
  await signIn(page);
  await openWorkbench(page);

  await page.getByPlaceholder(/MSH\|.*ADT\^A01/i).fill(SAMPLE_HL7);

  // Default required-segments is "MSH,PID" — leave it. Set the minimum version to 2.5.
  await page.getByPlaceholder(/Minimum version/i).fill("2.5");

  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/admin/workbench/validate-hl7") && r.request().method() === "POST",
    ),
    page.getByRole("button", { name: /^Validate$/ }).click(),
  ]);

  // The valid-result panel renders the ✓ Valid line.
  await expect(page.getByText(/Valid against the configured rules\./i)).toBeVisible({
    timeout: 10_000,
  });
});
