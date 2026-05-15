import { defineConfig, devices } from "@playwright/test";

// E2E tests run against the live Aspire stack:
//   dotnet run --project src/aspire/Dialysis.AppHost
// All requests go through the gateway origin so cookie + JWT round-trips behave the same
// way they do in a real browser session. There is intentionally no `webServer` block — the
// stack is too heavy (Keycloak, 5 module APIs, 5 Postgres containers) to launch from a
// per-test-suite hook. Tests skip themselves cleanly if the gateway is unreachable.
const baseURL = process.env.E2E_BASE_URL ?? "http://localhost:9090";

// Headed/Brave demo mode. Set E2E_HEADED=1 to watch the test execute in Brave (or any
// chromium-derivative pointed at by E2E_BROWSER_PATH). slowMo gives the human watcher a
// chance to follow the click → redirect → login → dashboard sequence.
const headed = process.env.E2E_HEADED === "1";
const bravePath =
  process.env.E2E_BROWSER_PATH ?? "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser";

export default defineConfig({
  testDir: "./e2e",
  timeout: 60_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  retries: 0,
  workers: 1,
  reporter: [["list"]],
  use: {
    baseURL,
    trace: "retain-on-failure",
    video: "retain-on-failure",
    screenshot: "only-on-failure",
    ignoreHTTPSErrors: true,
    headless: !headed,
    launchOptions: headed
      ? { executablePath: bravePath, slowMo: 400, args: ["--start-maximized"] }
      : {},
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
