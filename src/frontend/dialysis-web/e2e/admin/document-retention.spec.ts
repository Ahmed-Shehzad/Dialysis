import { expect, test } from "@playwright/test";
import { signIn } from "../helpers/signIn";

// Document retention CRUD round-trip — exercises the operator-mutable per-kind retention
// windows shipped alongside the DSR Art. 17 erasure pipeline.
//
// Self-contained: creates a uniquely-named kind so the production-clean stack has no row
// collisions. Cleans up via the Remove button at the end.
test.describe.configure({ timeout: 180_000 });

test("upsert + revise + remove a retention policy", async ({ page }) => {
  await signIn(page);
  await page.goto("/hie/admin/documents/retention");
  await expect(page.getByRole("heading", { name: /Document retention/i })).toBeVisible({
    timeout: 30_000,
  });

  const kind = `E2EKind${Date.now()}`;

  // Create.
  await page.getByRole("button", { name: /New policy/i }).click();
  await page.getByPlaceholder("DischargeLetter").fill(kind);
  await page.locator('input[type="number"]').fill("365");
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/documents/retention/policies/") && r.request().method() === "PUT",
    ),
    page.getByRole("button", { name: /^Save$/i }).click(),
  ]);
  const row = page.getByRole("row", { name: new RegExp(kind) });
  await expect(row).toBeVisible({ timeout: 15_000 });
  await expect(row).toContainText("365");

  // Revise the window.
  await row.getByRole("button", { name: /^Edit$/i }).click();
  await page.locator('input[type="number"]').fill("730");
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/documents/retention/policies/") && r.request().method() === "PUT",
    ),
    page.getByRole("button", { name: /^Save$/i }).click(),
  ]);
  await expect(page.getByRole("row", { name: new RegExp(kind) })).toContainText("730");

  // Remove.
  await page
    .getByRole("row", { name: new RegExp(kind) })
    .getByRole("button", { name: /^Edit$/i })
    .click();
  await Promise.all([
    page.waitForResponse(
      (r) =>
        r.url().includes("/documents/retention/policies/") && r.request().method() === "DELETE",
    ),
    page.getByRole("button", { name: /^Remove$/i }).click(),
  ]);
  await expect(page.getByRole("row", { name: new RegExp(kind) })).toHaveCount(0, {
    timeout: 15_000,
  });
});
