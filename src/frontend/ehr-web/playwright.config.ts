import { defineConfig, devices } from "@playwright/test";

/**
 * ehr-web UI end-to-end tests. They run against the Vite dev server (served under base `/ehr/`);
 * every BFF / identity call is mocked via `page.route` in the specs, so no gateway or backend is
 * required — the specs assert the SPA renders its landing context for an authenticated user. CI
 * starts the dev server through `webServer`; locally an already-running dev server is reused.
 */
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [["github"], ["html", { open: "never" }]] : "list",
  // The Vite dev server compiles modules on first request, so the initial authenticated render can
  // take several seconds on a cold CI runner — give web-first assertions generous headroom.
  timeout: 60_000,
  expect: { timeout: 15_000 },
  use: {
    baseURL: "http://localhost:5332/ehr/",
    trace: "on-first-retry",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: {
    command: "npm run dev",
    url: "http://localhost:5332/ehr/",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});
