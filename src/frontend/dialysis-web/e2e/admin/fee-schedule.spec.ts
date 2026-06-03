import { expect, test } from "@playwright/test";
import { signIn } from "../helpers/signIn";

// Full CRUD round-trip for the CPT fee-schedule admin page. Self-contained: creates its own
// row against a unique payer code, so it needs no seeded data and is safe against the
// production-clean stack.
//
//   New rate → fill CPT + payer + amount + effective date → Save → assert row → Edit (revise
//   amount) → Save → assert new amount → Delete → assert gone.
test.describe.configure({ timeout: 180_000 });

test("create, revise, and delete a CPT fee-schedule rate", async ({ page }) => {
  await signIn(page);
  await page.goto("/admin/billing/fee-schedule");
  await expect(page.getByRole("heading", { name: /CPT fee schedule/i })).toBeVisible({
    timeout: 30_000,
  });

  const payer = `E2E${Date.now()}`.slice(0, 20);
  const cpt = "90935";

  // Create.
  await page.getByRole("button", { name: /New rate/i }).click();
  await expect(page.getByRole("heading", { name: /New rate/i })).toBeVisible();
  await page.getByPlaceholder("90935").fill(cpt);
  await page.getByPlaceholder(/MED01 or \*/).fill(payer);
  await page.locator('input[type="number"]').fill("250");
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/billing/fee-schedule") && r.request().method() === "POST",
    ),
    page.getByRole("button", { name: /^Save$/i }).click(),
  ]);

  const row = page.getByRole("row", { name: new RegExp(payer) });
  await expect(row).toBeVisible({ timeout: 15_000 });
  await expect(row).toContainText("250.00");

  // Revise the amount.
  await row.getByRole("button", { name: /^Edit$/i }).click();
  await expect(page.getByRole("heading", { name: /Revise rate/i })).toBeVisible();
  await page.locator('input[type="number"]').fill("275");
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/billing/fee-schedule/") && r.request().method() === "PUT",
    ),
    page.getByRole("button", { name: /^Save$/i }).click(),
  ]);
  await expect(page.getByRole("row", { name: new RegExp(payer) })).toContainText("275.00");

  // Delete.
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/billing/fee-schedule/") && r.request().method() === "DELETE",
    ),
    page
      .getByRole("row", { name: new RegExp(payer) })
      .getByRole("button", { name: /^Delete$/i })
      .click(),
  ]);
  await expect(page.getByRole("row", { name: new RegExp(payer) })).toHaveCount(0, {
    timeout: 15_000,
  });
});
