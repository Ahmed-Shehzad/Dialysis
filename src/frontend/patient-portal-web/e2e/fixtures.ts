import { test as base, expect, type Page } from "@playwright/test";

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
 * Benign catch-all for any `/portal/api` call a secondary panel makes that a spec did not mock, so
 * those panels render empty instead of erroring. Register this BEFORE the specific routes — Playwright
 * matches the most-recently-registered handler first, so later, more specific routes take precedence.
 */
export async function stubApiCatchAll(page: Page): Promise<void> {
  await page.route("**/portal/api/**", (route) => route.fulfill({ json: { data: [], items: [] } }));
}

export const test = base;
export { expect };
