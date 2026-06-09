import { defineConfig, devices } from "@playwright/test";

/**
 * Full-system MVP demo configuration.
 *
 * Unlike the per-app e2e suites (which mock every BFF call and start their own Vite dev server),
 * this project drives the **live Aspire stack**: it talks to the real edge Gateway at
 * `http://localhost:9090`, authenticates through the real Keycloak realm, and tours every SPA + BFF
 * with real, DataSimulator-seeded data. There is therefore **no `webServer`** — the stack must
 * already be up (`dotnet run --project src/aspire/Dialysis.AppHost`).
 *
 * The single spec records one continuous video (>= 20 minutes) to the gitignored
 * `e2e-artifacts/mvp-demo/` tree. Captures are forced on; the run is paced deliberately (slowMo +
 * per-stop dwell) so the walkthrough is watchable end-to-end as an MVP demo.
 */
export default defineConfig({
  testDir: ".",
  testMatch: "mvp-demo.spec.ts",
  outputDir: "../e2e-artifacts/mvp-demo/test-results",
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: 0,
  reporter: [
    ["list"],
    ["html", { open: "never", outputFolder: "../e2e-artifacts/mvp-demo/report" }],
  ],
  // A >=20-minute walkthrough in one test — give it 40 minutes of headroom.
  timeout: 40 * 60 * 1000,
  expect: { timeout: 20_000 },
  use: {
    baseURL: process.env.DEMO_BASE_URL ?? "http://localhost:9090",
    ignoreHTTPSErrors: true,
    actionTimeout: 25_000,
    navigationTimeout: 60_000,
    trace: "off",
    screenshot: "on",
    viewport: { width: 1600, height: 900 },
    video: { mode: "on", size: { width: 1600, height: 900 } },
    launchOptions: {
      // Smooth, visible interactions so the recording reads as a guided demo, not a fast robot.
      slowMo: Number(process.env.DEMO_SLOWMO ?? 110),
    },
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
