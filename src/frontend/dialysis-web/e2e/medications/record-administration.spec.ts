import { expect, test } from "@playwright/test";
import { signIn } from "../helpers/signIn";

// MAR write golden path (PR 6 + PR 8 dialog). Drives:
//   open live session → Medications tab → "Record administration" → fill RxNorm + dose +
//   route → Save → assert the new row.
//
// Needs an open session to act on. The production-clean stack has none seeded, so the spec
// skips cleanly when the session list is empty rather than failing — the same fail-soft posture
// as probe-routes.spec.ts. When a session exists it runs the full round-trip.
test.describe.configure({ timeout: 180_000 });

test("record a medication administration on a live session", async ({ page }) => {
  await signIn(page);
  await page.goto("/sessions");
  await expect(page.getByRole("heading").first()).toBeVisible({ timeout: 30_000 });

  const openLink = page.getByRole("link", { name: /Open/i }).first();
  test.skip(
    !(await openLink.isVisible().catch(() => false)),
    "No open session in the list — nothing to record against on a production-clean stack.",
  );

  await openLink.click();
  await page.waitForURL(/\/sessions\/[0-9a-f-]+$/i, { timeout: 15_000 });

  // Switch to the Medications tab and open the dialog.
  await page.getByRole("button", { name: /^Medications$/i }).click();
  await page.getByRole("button", { name: /Record administration/i }).click();
  await expect(page.getByRole("heading", { name: /Record administration/i })).toBeVisible();

  // Fill display, RxNorm code, dose. Coding system defaults to RxNorm.
  await page.getByPlaceholder(/Heparin 5000 IU/i).fill("Heparin 5000 IU");
  await page.getByPlaceholder(/1234/).fill("1361574");
  await page.locator('input[type="number"]').first().fill("5000");

  // Save — POSTs the administration and the new MAR row appears.
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/medications") && r.request().method() === "POST",
    ),
    page.getByRole("button", { name: /^Save$/i }).click(),
  ]);

  await expect(page.getByText(/Heparin 5000 IU/i).first()).toBeVisible({ timeout: 15_000 });
});
