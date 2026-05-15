import { expect, test } from "@playwright/test";

// Diagnostic: sign in, grab the JWT the BFF hands the SPA, decode it, and print the
// audience + issuer so we can confirm whether the gateway should be accepting it.
test("dump access token audience + issuer after live sign-in", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("button", { name: /sign in/i }).click();
  await page.waitForURL(/realms\/dialysis\/protocol\/openid-connect\/auth/i);
  await page.locator("#username").fill("demo");
  await page.locator("#password").fill("demo");
  await Promise.all([
    page.waitForURL((u) => u.host === "localhost:9090" && u.pathname === "/"),
    page.locator("#kc-login, input[name=login], button[name=login]").first().click(),
  ]);

  // /identity/user from inside the page → BFF cookie is auto-sent.
  const body = await page.evaluate(async () => {
    const r = await fetch("/identity/user", { credentials: "include" });
    return { status: r.status, json: r.ok ? await r.json() : null };
  });
  expect(body.status, "BFF /identity/user failed in-page").toBe(200);
  const at = (body.json as { accessToken?: string } | null)?.accessToken;
  expect(typeof at).toBe("string");

  // Decode the payload (same approach as the SPA decoder).
  const payload = JSON.parse(
    Buffer.from(at!.split(".")[1].replaceAll("-", "+").replaceAll("/", "_"), "base64").toString(
      "utf-8",
    ),
  ) as Record<string, unknown>;

  console.log("\n=== FULL ACCESS TOKEN PAYLOAD ===");
  console.log(JSON.stringify(payload, null, 2));
  console.log("=================================\n");

  // Now hit /api/his/* directly from inside the page and capture status + response.
  const apiResult = await page.evaluate(async (token) => {
    const r = await fetch("/api/his/api/v1.0/data-management/manager-dashboard", {
      headers: { Authorization: "Bearer " + token, Accept: "application/json" },
    });
    const text = await r.text();
    return {
      status: r.status,
      body: text.slice(0, 300),
      wwwAuth: r.headers.get("www-authenticate"),
    };
  }, at);
  console.log("=== /api/his GET ===");
  console.log("status:", apiResult.status);
  console.log("www-authenticate:", apiResult.wwwAuth);
  console.log("body[:300]:", apiResult.body);
  console.log("====================\n");
});
