import { expect, test } from "@playwright/test";
import { signIn } from "../helpers/signIn";

// Full authoring round-trip for the multi-language reporting templates (PR 12). Self-contained:
// the test creates its own template with a unique slug, so it needs no seeded data and is safe
// against the production-clean stack.
//
//   New template → fill slug + title + language + body → Save draft → reopen → Publish v1 →
//   assert the "Published" badge.
test.describe.configure({ timeout: 180_000 });

test("author a German discharge-letter template, publish version 1", async ({ page }) => {
  await signIn(page);
  await page.goto("/admin/reporting/templates");
  await expect(page.getByRole("heading", { name: /Reporting templates/i })).toBeVisible({
    timeout: 30_000,
  });

  const slug = `e2e-discharge-de-${Date.now()}`;
  const title = `E2E Entlassungsbrief ${Date.now()}`;

  // Open the New template drawer and fill it.
  await page.getByRole("button", { name: /New template/i }).click();
  await expect(page.getByRole("heading", { name: /New template/i })).toBeVisible();
  await page.getByPlaceholder(/discharge-letter-de/i).fill(slug);
  await page.getByPlaceholder(/Entlassungsbrief/i).fill(title);
  await page.getByPlaceholder(/leave blank for default/i).fill("de");
  await page
    .getByPlaceholder(/patient\.name/i)
    .fill("# {{patient.name}}\n\nBehandlung am {{session.completed}} abgeschlossen.");

  // Save the draft — POSTs to /reporting/templates and the row appears.
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/reporting/templates") && r.request().method() === "POST",
    ),
    page.getByRole("button", { name: /Save draft/i }).click(),
  ]);
  await expect(page.getByText(title, { exact: true })).toBeVisible({ timeout: 15_000 });

  // Open the version drawer and publish version 1.
  await page.getByText(title, { exact: true }).click();
  await Promise.all([
    page.waitForResponse(
      (r) =>
        r.url().includes(`/reporting/templates/${slug}/publish`) && r.request().method() === "POST",
    ),
    page
      .getByRole("button", { name: /^Publish$/i })
      .first()
      .click(),
  ]);

  // The version is now flagged Published.
  await expect(page.getByText(/Published/i).first()).toBeVisible({ timeout: 15_000 });
});
