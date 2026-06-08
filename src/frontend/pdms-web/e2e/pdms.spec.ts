import { expect, mockAuth, stubApiCatchAll, stubRealtime, test } from "./fixtures";

test.describe("pdms-web — authenticated landing", () => {
  test("loads the pdms landing context for an authenticated user", async ({ page }) => {
    await stubRealtime(page);
    await stubApiCatchAll(page);
    await mockAuth(page, { permissions: ["pdms.treatment_sessions.view"] });

    await page.goto("/pdms/");

    // The module shell renders for an authenticated user — i.e. the SPA got past the login gate,
    // resolved the BFF identity probe, and routed to its landing context. (Data-level rendering is
    // exercised by patient-portal-web's S16 spec; the lazy page bodies are subject to Vite
    // dev-server cold-start import races, so the SPA-coverage specs anchor on the stable shell.)
    await expect(page.getByRole("heading", { name: "Chairside" })).toBeVisible();
  });
});
