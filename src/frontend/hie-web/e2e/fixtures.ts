import { test as base, expect, type Page } from "@playwright/test";

/** Minimal shape of the BFF `/hie/identity/user` probe response stubbed in tests. */
export interface MockUser {
  name?: string;
  permissions?: string[];
  claims?: Record<string, unknown>;
}

/** Stubs the BFF auth probe so the SPA renders as an authenticated user with the given permissions. */
export async function mockAuth(page: Page, user: MockUser = {}): Promise<void> {
  await page.route("**/hie/identity/user", (route) =>
    route.fulfill({
      json: {
        name: user.name ?? "demo-staff",
        roles: [],
        permissions: user.permissions ?? [],
        claims: user.claims ?? {},
        accessToken: "test-token",
      },
    }),
  );
}

/** Aborts SignalR negotiate / hub traffic — there is no realtime backend in these mocked e2e runs. */
export async function stubRealtime(page: Page): Promise<void> {
  await page.route("**/hie/hubs/**", (route) => route.abort());
  await page.route("**/hie/events/**", (route) => route.abort());
}

/**
 * Aborts any `/hie/api` call a panel makes that a spec did not explicitly mock, so the SPA renders
 * its friendly per-panel error state (humanizeError) instead of crashing the page. Register this BEFORE
 * the specific routes — Playwright matches the most-recently-registered handler first.
 */
export async function stubApiCatchAll(page: Page): Promise<void> {
  await page.route("**/hie/api/**", (route) => route.abort());
}

export const test = base;
export { expect };
