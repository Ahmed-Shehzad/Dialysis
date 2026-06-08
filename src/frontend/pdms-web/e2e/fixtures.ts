import { readFileSync } from "node:fs";
import { join } from "node:path";
import { type Page } from "@playwright/test";

/** Minimal shape of the BFF `/pdms/identity/user` probe response stubbed in tests. */
export interface MockUser {
  name?: string;
  permissions?: string[];
  claims?: Record<string, unknown>;
}

/** Stubs the BFF auth probe so the SPA renders as an authenticated user with the given permissions. */
export async function mockAuth(page: Page, user: MockUser = {}): Promise<void> {
  await page.route("**/pdms/identity/user", (route) =>
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
  await page.route("**/pdms/hubs/**", (route) => route.abort());
  await page.route("**/pdms/events/**", (route) => route.abort());
}

/**
 * Aborts any `/pdms/api` call a panel makes that a spec did not explicitly mock, so the SPA renders
 * its friendly per-panel error state (humanizeError) instead of crashing the page. Register this BEFORE
 * the specific routes — Playwright matches the most-recently-registered handler first.
 */
export async function stubApiCatchAll(page: Page): Promise<void> {
  await page.route("**/pdms/api/**", (route) => route.abort());
}

/** Reads a committed sample PDF from this app's e2e/fixtures/ directory (cwd is the app root). */
export function fixturePdf(name: string): Buffer {
  return readFileSync(join(process.cwd(), "e2e", "fixtures", name));
}

/**
 * Deep-links to a live session and opens its "Documents" tab (which hosts SessionReportsTab). The
 * session page is a `React.lazy` chunk; on a cold Vite dev server the first dynamic import can lose a
 * race and fail, so if the tab doesn't appear quickly we reload once — by then Vite has compiled the
 * chunk. Returns with the Documents tab selected.
 */
export async function gotoSessionDocuments(page: Page, sessionId: string): Promise<void> {
  await page.goto(`/pdms/sessions/${sessionId}`);
  const docsTab = page.getByRole("button", { name: "Documents" });
  try {
    await docsTab.waitFor({ state: "visible", timeout: 8_000 });
  } catch {
    await page.reload();
    await docsTab.waitFor({ state: "visible", timeout: 20_000 });
  }
  await docsTab.click();
}

export { expect, test } from "@playwright/test";
