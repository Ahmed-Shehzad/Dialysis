// Throwaway login smoke — validates the real Keycloak OIDC flow + a couple of pages before the long run.
import { chromium } from "@playwright/test";

const USER = "demo";
const PASS = "demo";

async function signInIfNeeded(page) {
  for (let i = 0; i < 7; i++) {
    await page.waitForTimeout(1800); // let the redirect / SPA render settle before deciding
    const u = new URL(page.url());
    const onKc =
      u.host.includes("8081") || u.pathname.includes("/realms/") || u.pathname.includes("/auth/");
    const onLogin = /\/login\/?$/.test(u.pathname);

    if (onKc) {
      const kc = page.locator("#username, input[name=username]").first();
      if (await kc.isVisible().catch(() => false)) {
        await kc.fill(USER);
        await page.locator("#password, input[name=password]").first().fill(PASS);
        await page
          .locator("#kc-login, button[type=submit], input[type=submit]")
          .first()
          .click()
          .catch(() => page.keyboard.press("Enter"));
        await page.waitForLoadState("load").catch(() => {});
      }
      continue; // transient KC redirect page → loop and re-evaluate
    }
    if (onLogin) {
      const launcher = page
        .getByRole("button", { name: /sign in|log ?in|continue|single sign/i })
        .or(page.getByRole("link", { name: /sign in|log ?in|continue|single sign/i }))
        .first();
      if (await launcher.isVisible().catch(() => false)) {
        await launcher.click().catch(() => {});
        await page.waitForLoadState("load").catch(() => {});
      }
      continue;
    }
    return; // genuinely on an app route → authenticated
  }
}

const b = await chromium.launch();
const ctx = await b.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1600, height: 900 } });
const page = await ctx.newPage();

for (const [ctxName, url, file] of [
  ["his", "http://localhost:9090/his/today", "/tmp/smoke-his.png"],
  ["ehr", "http://localhost:9090/ehr/patients", "/tmp/smoke-ehr.png"],
  ["pdms", "http://localhost:9090/pdms/sessions", "/tmp/smoke-pdms.png"],
  ["hie", "http://localhost:9090/hie/admin/documents", "/tmp/smoke-hie.png"],
]) {
  await page.goto(url, { waitUntil: "domcontentloaded" }).catch(() => {});
  await signInIfNeeded(page);
  await page.waitForLoadState("networkidle").catch(() => {});
  await page.waitForTimeout(2500);
  await page.screenshot({ path: file, fullPage: false });
  const body = (await page.locator("body").innerText().catch(() => "")) || "";
  console.log(`${ctxName.padEnd(5)} url=${page.url().slice(0, 60)} textLen=${body.trim().length}`);
}

await b.close();
console.log("smoke done");
