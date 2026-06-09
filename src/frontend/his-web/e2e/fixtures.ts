import { type Page } from "@playwright/test";

/** Minimal shape of the BFF `/his/identity/user` probe response stubbed in tests. */
export interface MockUser {
  name?: string;
  permissions?: string[];
  claims?: Record<string, unknown>;
}

/** Stubs the BFF auth probe so the SPA renders as an authenticated user with the given permissions. */
export async function mockAuth(page: Page, user: MockUser = {}): Promise<void> {
  await page.route("**/his/identity/user", (route) =>
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
  await page.route("**/his/hubs/**", (route) => route.abort());
  await page.route("**/his/events/**", (route) => route.abort());
}

/**
 * Aborts any `/his/api` call a panel makes that a spec did not explicitly mock, so the SPA renders
 * its friendly per-panel error state (humanizeError) instead of crashing the page. Register this BEFORE
 * the specific routes — Playwright matches the most-recently-registered handler first.
 */
export async function stubApiCatchAll(page: Page): Promise<void> {
  await page.route("**/his/api/**", (route) => route.abort());
}

/**
 * Navigates to a lazy-loaded workflow route and waits for its anchor heading. The per-context pages
 * are `React.lazy` chunks; on a cold Vite dev server the first dynamic import for a chunk can lose a
 * race and fail. If the heading doesn't appear quickly we reload once — by then Vite has compiled the
 * chunk, so the second navigation renders reliably.
 */
export async function gotoWorkflow(page: Page, path: string, headingName: string): Promise<void> {
  await page.goto(path);
  const heading = page.getByRole("heading", { name: headingName }).first();
  try {
    await heading.waitFor({ state: "visible", timeout: 8_000 });
  } catch {
    await page.reload();
    await heading.waitFor({ state: "visible", timeout: 20_000 });
  }
}

export { expect, test } from "@playwright/test";
