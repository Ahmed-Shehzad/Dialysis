import { expect, mockAuth, stubApiCatchAll, stubRealtime, test } from "./fixtures";

// A fixed demo patient id the discovery endpoint returns and the summary is keyed to. The whole
// point of S16 is that the patient the page *discovers* is the same one whose summary it renders.
const DEMO_PATIENT = "11111111-1111-4111-8111-111111111111";

test.describe("patient portal — S16 discovery → related summary", () => {
  test("discovers a portal patient and renders that patient's summary", async ({ page }) => {
    await stubRealtime(page);
    await stubApiCatchAll(page);
    await mockAuth(page);

    // S16 discovery: HIS PatientAccess lists patient ids that have portal data (HATEOAS envelope).
    await page.route(
      (url) => url.pathname.endsWith("/patient-access/patients"),
      (route) => route.fulfill({ json: { data: [DEMO_PATIENT], links: [] } }),
    );

    // The summary for the discovered patient — the related counts the portal renders for it.
    await page.route(
      (url) => url.pathname.endsWith(`/patient-access/patients/${DEMO_PATIENT}/portal-summary`),
      (route) =>
        route.fulfill({
          json: {
            data: {
              patientId: DEMO_PATIENT,
              upcomingAppointmentCount: 2,
              openMedicationOrderCount: 3,
              openAdmissionCount: 1,
            },
            links: [],
          },
        }),
    );

    await page.goto("/");

    // The portal header renders (we are past the auth gate, not on the login screen).
    await expect(page.getByRole("heading", { name: /your portal/i })).toBeVisible();

    // The discovery selector is populated with the discovered patient and auto-selected (S16).
    const selector = page.getByLabel("Patient with portal data");
    await expect(selector).toBeVisible();
    await expect(selector).toHaveValue(DEMO_PATIENT);

    // The summary tiles render the related counts keyed to THAT patient — consistency + relatedness:
    // the figures shown are the ones the summary endpoint returned for the discovered id.
    const summary = page.getByRole("region", { name: /portal summary/i });
    await expect(summary).toContainText("Upcoming appointments");
    await expect(summary).toContainText("Open medications");
    await expect(summary.getByText("2", { exact: true })).toBeVisible();
    await expect(summary.getByText("3", { exact: true })).toBeVisible();
  });
});
