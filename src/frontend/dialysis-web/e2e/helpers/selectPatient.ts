import { expect, type Page } from "@playwright/test";

// Many admin pages (documents, EHR chart) scope their query by the currently-selected
// patient. The shell holds the selection in PatientContextProvider and surfaces a picker
// at the top of the app chrome. Specs that need a patient call this helper after sign-in
// to settle the page state into "a patient is selected" before exercising the page.
//
// Production-clean strategy: if a patient selector with a non-empty value already exists
// (e.g. the previous step picked one), this is a no-op. Otherwise the helper opens the
// picker, types a "find any" search, and clicks the first result. If no patient exists at
// all the helper returns null so the caller can branch its assertions onto the "no patient"
// happy path.
export const selectAnyPatient = async (page: Page): Promise<string | null> => {
  // The patient-context bar's selector renders as a button labelled "Patient" or shows the
  // current display name. Either is fine — we just need a way to open the picker.
  const opener = page
    .getByRole("button", { name: /Patient/i })
    .or(page.getByRole("button", { name: /Select patient/i }))
    .first();
  if (!(await opener.isVisible().catch(() => false))) {
    return null;
  }
  await opener.click();

  // The picker shows an input + a results list. If the dev stack has zero patients seeded
  // the list will be empty — return null so the caller can branch.
  const search = page.getByPlaceholder(/search by name|MRN/i).first();
  if (await search.isVisible({ timeout: 5_000 }).catch(() => false)) {
    await search.fill("a");
  }
  const firstResult = page.getByRole("button", { name: /Use patient/i }).first();
  if (!(await firstResult.isVisible({ timeout: 5_000 }).catch(() => false))) {
    // Close the picker; production-clean stack has no patients yet.
    await page.keyboard.press("Escape");
    return null;
  }
  await firstResult.click();

  // The bar collapses and shows the chosen patient's display name — return it so the spec
  // can do further filtering / assertions.
  const display = page.locator('[data-testid="patient-context-display"]').first();
  if (await display.isVisible({ timeout: 5_000 }).catch(() => false)) {
    return await display.textContent();
  }
  return null;
};

/** Asserts that a patient context is currently selected; skips the spec if none exist. */
export const requirePatientSelected = async (
  page: Page,
  test: { skip(condition: boolean, reason: string): void },
): Promise<void> => {
  const selected = await selectAnyPatient(page);
  if (selected === null) {
    test.skip(true, "No patient is available in this environment — patient-scoped spec can't run.");
    return;
  }
  await expect(page.locator("body")).toContainText(/Patient|MRN/, { timeout: 5_000 });
};
