import { test, expect } from "@playwright/test";

const BASE_URL = "http://localhost:5173";

test.describe("Real-time updates (live SignalR)", () => {
    test("Real-Time Monitoring: subscribe flow shows status", async ({
        page,
    }) => {
        await page.goto(BASE_URL);

        await expect(
            page.getByRole("heading", { name: /Dialysis PDMS Dashboard/i }),
        ).toBeVisible();

        const subscribeBtn = page.getByRole("button", { name: /Subscribe/i });
        await expect(subscribeBtn).toBeVisible();
        await expect(subscribeBtn).toBeDisabled();

        const sessionInput = page.getByPlaceholder(/e.g. THERAPY/i);
        await sessionInput.fill("THERAPYTEST123");

        await expect(subscribeBtn).toBeEnabled();
        await subscribeBtn.click();

        await expect(
            page.getByText(/Connecting…|Connected|SignalR/),
        ).toBeVisible({ timeout: 10000 });
    });

    test("Stats cards are visible", async ({ page }) => {
        await page.goto(BASE_URL);

        await expect(
            page.getByRole("heading", { name: /Dialysis PDMS Dashboard/i }),
        ).toBeVisible();

        await expect(page.locator("text=Sessions Summary").first()).toBeVisible(
            { timeout: 5000 },
        );
        await expect(
            page.locator("text=Alarms by Severity").first(),
        ).toBeVisible();
        await expect(
            page.locator("text=Prescription Compliance").first(),
        ).toBeVisible();
    });

    test("Real-Time Monitoring section is present", async ({ page }) => {
        await page.goto(BASE_URL);

        await expect(
            page.getByRole("heading", { name: /Real-Time Monitoring/i }),
        ).toBeVisible();
    });
});
