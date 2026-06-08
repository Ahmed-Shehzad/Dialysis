import { expect, mockAuth, stubApiCatchAll, stubRealtime, test } from "./fixtures";

// A fixed demo patient id the discovery endpoint returns. S16's point: a staff/dev session with no
// patient claim can *discover* a real, simulator-populated patient and surface it in the UI.
const DEMO_PATIENT = "11111111-1111-4111-8111-111111111111";

test.describe("patient portal — S16 discovery", () => {
  test("surfaces a discovered portal patient in the selector", async ({ page }) => {
    await stubRealtime(page);
    await stubApiCatchAll(page);
    await mockAuth(page);

    // S16 discovery: HIS PatientAccess lists patient ids that have portal data (HATEOAS envelope).
    // Everything else under /portal/api is aborted by the catch-all → friendly per-panel error states,
    // so the page stays mounted and the assertion targets only the discovery surface.
    await page.route(
      (url) => url.pathname.endsWith("/patient-access/patients"),
      (route) => route.fulfill({ json: { data: [DEMO_PATIENT], links: [] } }),
    );

    await page.goto("/portal/");

    // We are past the auth gate (the authenticated portal, not the login screen).
    await expect(page.getByRole("heading", { name: /your portal/i })).toBeVisible();

    // The discovered patient is surfaced in the UI selector — proving the SPA consumed the discovery
    // endpoint and a claim-less session can open a real patient. (The summary half of S16 — loading
    // that patient's related counts — is asserted server-side in simulator-smoke.yml.)
    const selector = page.getByLabel("Patient with portal data");
    await expect(selector).toBeVisible();
    await expect(selector.locator("option")).toHaveText(DEMO_PATIENT);
  });
});
