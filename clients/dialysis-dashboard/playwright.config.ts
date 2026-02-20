import { defineConfig, devices } from "@playwright/test";

/**
 * Playwright E2E tests for the Dialysis Dashboard.
 * Prerequisites: React app running at http://localhost:5173 (npm run dev).
 * Run: npx playwright install && npm run test:e2e
 */
export default defineConfig({
    testDir: "./e2e",
    fullyParallel: false,
    forbidOnly: !!process.env.CI,
    retries: 0,
    workers: 1,
    reporter: "list",
    use: {
        baseURL: "http://localhost:5173",
        trace: "on-first-retry",
        screenshot: "only-on-failure",
        video: "retain-on-failure",
    },
    projects: [
        {
            name: "chromium",
            use: { ...devices["Desktop Chrome"] },
        },
    ],
    webServer: undefined,
});
