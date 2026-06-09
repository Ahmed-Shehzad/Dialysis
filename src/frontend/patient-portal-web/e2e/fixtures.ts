import { type Page } from "@playwright/test";

/** Minimal shape of the BFF `/portal/identity/user` probe response stubbed in tests. */
export interface PortalUser {
  name?: string;
  permissions?: string[];
  claims?: Record<string, unknown>;
}

/**
 * Stubs the BFF auth probe. With no `his_patient_id` claim the portal falls into discovery mode —
 * the path scenario S16 exercises (staff/dev session picking a patient).
 */
export async function mockAuth(page: Page, user: PortalUser = {}): Promise<void> {
  await page.route("**/portal/identity/user", (route) =>
    route.fulfill({
      json: {
        name: user.name ?? "demo-staff",
        roles: [],
        permissions: user.permissions ?? ["his.patientaccess.portal.read"],
        claims: user.claims ?? {},
        accessToken: "test-token",
      },
    }),
  );
}

/** Aborts SignalR negotiate / hub traffic — there is no realtime backend in these mocked e2e runs. */
export async function stubRealtime(page: Page): Promise<void> {
  await page.route("**/portal/hubs/**", (route) => route.abort());
  await page.route("**/portal/events/**", (route) => route.abort());
}

/**
 * Aborts any `/portal/api` call a secondary panel makes that a spec did not explicitly mock. The SPA
 * renders a friendly per-panel error state (via humanizeError) on a failed request, so aborting keeps
 * the page mounted without us having to model every panel's response shape (a wrong-shaped 200 can
 * crash a panel and, with no error boundary, unmount the whole page). Register this BEFORE the
 * specific routes — Playwright matches the most-recently-registered handler first, so later, more
 * specific routes take precedence.
 */
export async function stubApiCatchAll(page: Page): Promise<void> {
  await page.route("**/portal/api/**", (route) => route.abort());
}

export { expect, test } from "@playwright/test";
