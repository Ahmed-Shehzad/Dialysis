import { expect, type Page } from "@playwright/test";

// Shared Keycloak-OIDC sign-in helper, extracted from smartconnect/new-channel.spec.ts so
// every new admin-page spec drives the same login flow. Credentials default to the demo realm
// user; override via E2E_KC_USERNAME / E2E_KC_PASSWORD for a different realm seed.
const KEYCLOAK_USERNAME = process.env.E2E_KC_USERNAME ?? "demo";
const KEYCLOAK_PASSWORD = process.env.E2E_KC_PASSWORD ?? "demo";

export const signIn = async (page: Page): Promise<void> => {
  await page.goto("/");
  await expect(page.getByRole("button", { name: /sign in/i })).toBeVisible({ timeout: 15_000 });
  await page.getByRole("button", { name: /sign in/i }).click();

  await page.waitForURL(/realms\/dialysis\/protocol\/openid-connect\/auth/i, { timeout: 15_000 });
  await page.locator("#username").fill(KEYCLOAK_USERNAME);
  await page.locator("#password").fill(KEYCLOAK_PASSWORD);
  await Promise.all([
    page.waitForURL((url) => url.host === "localhost:9090" && url.pathname === "/", {
      timeout: 60_000,
    }),
    page.locator("#kc-login, input[name=login], button[name=login]").first().click(),
  ]);
};
