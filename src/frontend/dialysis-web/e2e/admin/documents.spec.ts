import { expect, test } from "@playwright/test";
import { signIn } from "../helpers/signIn";
import { minimalPdfUpload } from "../helpers/minimalPdf";
import { selectAnyPatient } from "../helpers/selectPatient";

// Documents admin board (PRs #128 + #129) — list + upload + soft-delete round-trip.
//
// The page is patient-scoped: uploads require a patient to be selected in the shell's
// PatientContextProvider. On a production-clean stack with no seeded patients the upload
// half of the spec gracefully skips; the load + filter half still runs (proves the page
// renders, the list query fires, and the filter selects toggle).
//
//   Sign in → open /hie/admin/documents → assert heading and the four filter controls
//   → toggle each filter → branch:
//       (a) patient available → upload minimal PDF → row appears → open drawer →
//           "Mark entered-in-error" → row leaves the Current list (status filter still set).
//       (b) no patient        → assert the amber "Pick a patient" banner.
test.describe.configure({ timeout: 180_000 });

test("documents board loads, filters toggle, and round-trips an upload + soft-delete", async ({
  page,
}) => {
  await signIn(page);
  await page.goto("/hie/admin/documents");
  await expect(page.getByRole("heading", { name: /^Documents$/i })).toBeVisible({
    timeout: 30_000,
  });

  // The filter row is data-independent — always present, always interactive.
  const statusFilter = page.locator("select").nth(0);
  const sourceFilter = page.locator("select").nth(1);
  const kindFilter = page.getByPlaceholder(/Filter by kind/i);
  await expect(statusFilter).toBeVisible();
  await expect(sourceFilter).toBeVisible();
  await expect(kindFilter).toBeVisible();
  await sourceFilter.selectOption("PdmsReporting");
  await sourceFilter.selectOption("all");
  await kindFilter.fill("nonexistent-kind");
  await kindFilter.fill("");

  const patientDisplay = await selectAnyPatient(page).catch(() => null);
  if (patientDisplay === null) {
    // No patients seeded — assert the banner and exit.
    await expect(page.getByText(/Pick a patient from the top bar/i)).toBeVisible();
    return;
  }

  // Upload a minimal PDF and assert the row lands on the list.
  const upload = minimalPdfUpload();
  const fileInput = page.locator('input[type="file"]');
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/api/v1.0/documents") && r.request().method() === "POST",
    ),
    fileInput.setInputFiles(upload),
  ]);

  const row = page.getByRole("row", { name: new RegExp(upload.name) });
  await expect(row).toBeVisible({ timeout: 15_000 });
  await expect(row).toContainText("Admin upload");

  // Open the drawer + soft-delete.
  await row.getByRole("button", { name: /^Open$/i }).click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/api/v1.0/documents/") && r.request().method() === "DELETE",
    ),
    page.getByRole("button", { name: /entered-in-error/i }).click(),
  ]);

  // Status filter still on "Current" → the row should no longer be in the list.
  await expect(page.getByRole("row", { name: new RegExp(upload.name) })).toHaveCount(0, {
    timeout: 15_000,
  });
});
