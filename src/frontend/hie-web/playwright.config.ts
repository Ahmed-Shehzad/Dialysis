import { defineConfig, devices } from "@playwright/test";

/**
 * hie-web UI end-to-end tests. They run against the Vite dev server (served under base `/hie/`);
 * every BFF / identity call is mocked via `page.route` in the specs, so no gateway or backend is
 * required — the specs assert the SPA renders its landing context for an authenticated user, and the
 * document workflow walkthroughs (AcroForm fill, signing) drive the real PdfViewerDrawer over mocked
 * APIs. CI starts the dev server through `webServer`; locally an already-running dev server is reused.
 *
 * Artifacts (per-test video + screenshot + trace, and the HTML report) are written OUTSIDE src/ to
 * `e2e-artifacts/hie-web/` (gitignored). Playwright clears `outputDir` at the start of every run, so
 * each run overwrites the previous run's artifacts — bounded disk use, always the latest walkthrough.
 */
export default defineConfig({
  testDir: "./e2e",
  outputDir: "../../../e2e-artifacts/hie-web/test-results",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI
    ? [
        ["github"],
        ["html", { open: "never", outputFolder: "../../../e2e-artifacts/hie-web/report" }],
      ]
    : [
        ["list"],
        ["html", { open: "never", outputFolder: "../../../e2e-artifacts/hie-web/report" }],
      ],
  // The Vite dev server compiles modules on first request, so the initial authenticated render can
  // take several seconds on a cold CI runner — give web-first assertions generous headroom.
  timeout: 60_000,
  expect: { timeout: 15_000 },
  use: {
    baseURL: "http://localhost:5335/hie/",
    trace: "on-first-retry",
    // Record a video and capture a screenshot for every test (not just failures) so the workflow
    // walkthroughs are watchable end-to-end.
    video: "on",
    screenshot: "on",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: {
    command: "npm run dev",
    url: "http://localhost:5335/hie/",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});
