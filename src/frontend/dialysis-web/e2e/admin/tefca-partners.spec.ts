import { expect, test } from "@playwright/test";
import { signIn } from "../helpers/signIn";

// TEFCA QHIN partners — onboard + status round-trip. Self-contained: creates a uniquely-named
// partner so the production-clean stack has no collisions.
test.describe.configure({ timeout: 180_000 });

test("onboard a QHIN partner and revise its FHIR base URL", async ({ page }) => {
  await signIn(page);
  await page.goto("/hie/admin/tefca/partners");
  await expect(page.getByRole("heading", { name: /TEFCA QHIN partners/i })).toBeVisible({
    timeout: 30_000,
  });

  const name = `E2E-QHIN-${Date.now()}`;
  const base1 = "https://qhin-e2e.example/fhir";
  const ias = "https://qhin-e2e.example/ias";

  // Onboard.
  await page.getByRole("button", { name: /Onboard partner/i }).click();
  await page.getByPlaceholder(/https:\/\/qhin.example\/fhir/i).fill(base1);
  await page.getByPlaceholder(/https:\/\/qhin.example\/ias/i).fill(ias);
  // The name input is the first text input in the drawer; locate by its preceding label.
  await page.locator('input[type="text"]').first().fill(name);
  await Promise.all([
    page.waitForResponse(
      (r) =>
        r.url().includes("/tefca/partners") && r.request().method() === "POST" && r.status() < 400,
    ),
    page.getByRole("button", { name: /^Save$/i }).click(),
  ]);

  const row = page.getByRole("row", { name: new RegExp(name) });
  await expect(row).toBeVisible({ timeout: 15_000 });
  await expect(row).toContainText("Onboarding");

  // Revise the FHIR base URL via Edit.
  await row.getByRole("button", { name: /^Edit$/i }).click();
  const base2 = "https://qhin-e2e.example/fhir-v2";
  await page.getByPlaceholder(/https:\/\/qhin.example\/fhir/i).fill(base2);
  await Promise.all([
    page.waitForResponse(
      (r) =>
        r.url().includes("/tefca/partners/") && r.request().method() === "PUT" && r.status() < 400,
    ),
    page.getByRole("button", { name: /^Save$/i }).click(),
  ]);
  await expect(page.getByRole("row", { name: new RegExp(name) })).toContainText("fhir-v2");
});
