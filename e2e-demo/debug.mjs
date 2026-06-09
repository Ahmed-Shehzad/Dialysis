import { chromium } from "@playwright/test";

const b = await chromium.launch();
const ctx = await b.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1600, height: 900 } });
const page = await ctx.newPage();
const log = (m) => console.log(`[${Date.now() % 100000}] ${m}`);

await page.goto("http://localhost:9090/his/today", { waitUntil: "domcontentloaded" }).catch((e) => log("goto err " + e));
log("after goto: " + page.url());
await page.waitForTimeout(2500);
log("after wait: " + page.url());

const launcher = page.getByRole("button", { name: /sign in/i }).first();
log("launcher visible? " + (await launcher.isVisible().catch(() => "err")));
await launcher.click().catch((e) => log("click err " + e));
log("after click (immediate): " + page.url());
await page.waitForTimeout(4000);
log("after click +4s: " + page.url());
await page.screenshot({ path: "/tmp/dbg-1.png" });

const kc = page.locator("#username, input[name=username]").first();
log("kc username visible? " + (await kc.isVisible().catch(() => "err")));
log("page title: " + (await page.title().catch(() => "?")));
const bodyText = (await page.locator("body").innerText().catch(() => "")).slice(0, 300).replace(/\n/g, " | ");
log("body: " + bodyText);

if (await kc.isVisible().catch(() => false)) {
  await kc.fill("demo");
  await page.locator("#password, input[name=password]").first().fill("demo");
  await page.screenshot({ path: "/tmp/dbg-2-filled.png" });
  await page.locator("#kc-login, button[type=submit], input[type=submit]").first().click().catch((e) => log("submit err " + e));
  log("after submit (immediate): " + page.url());
  await page.waitForTimeout(5000);
  log("after submit +5s: " + page.url());
  await page.screenshot({ path: "/tmp/dbg-3-after.png" });
  log("final body: " + (await page.locator("body").innerText().catch(() => "")).slice(0, 200).replace(/\n/g, " | "));
}

await b.close();
log("debug done");
