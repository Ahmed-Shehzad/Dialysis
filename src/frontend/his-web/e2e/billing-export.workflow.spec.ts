import { test, expect, mockAuth, stubRealtime, stubApiCatchAll, gotoWorkflow } from "./fixtures";

/**
 * Billing-export workflow walkthrough (recorded to video): the operator opens the billing-export job
 * queue, sees a queued payer window, and clicks Execute — which re-dispatches the job to EHR to
 * assemble + submit the EDI 837 batch. The job status advances from Queued to Completed on the
 * refetch. Backend fully mocked (the Execute POST returns 202; the list flips Completed afterwards).
 */

const JOB_ID = "7c2f9a10-0000-4000-8000-exportjob01";

const job = (executed: boolean) => ({
  id: JOB_ID,
  payerCode: "MEDICARE",
  statusCode: executed ? "Completed" : "Queued",
  periodStart: "2026-06-01",
  periodEnd: "2026-06-30",
  submittedAtUtc: "2026-06-08T14:30:00Z",
  completedAtUtc: executed ? "2026-06-08T15:05:00Z" : null,
  notes: executed ? "837P batch submitted (2 claims)" : null,
});

test.describe("his-web — billing-export workflow", () => {
  test("queued job → Execute → status advances to Completed", async ({ page }) => {
    await stubRealtime(page);
    await mockAuth(page, { permissions: ["his.operations.billing.execute"] });
    await stubApiCatchAll(page);

    let executed = false;
    await page.route("**/his/api/v1.0/operations/billing/export-jobs**", async (route) => {
      const request = route.request();
      const path = new URL(request.url()).pathname;
      if (request.method() === "POST" && path.endsWith("/execute")) {
        executed = true;
        return route.fulfill({ status: 202, body: "" });
      }
      if (request.method() === "GET" && path.endsWith("/export-jobs")) {
        return route.fulfill({ json: [job(executed)] });
      }
      return route.fallback();
    });

    await gotoWorkflow(page, "/his/admin/billing/exports", "Billing export jobs");

    // A queued payer window is listed with an Execute action.
    await expect(page.getByText("MEDICARE")).toBeVisible();
    await expect(page.getByText("2026-06-01 → 2026-06-30")).toBeVisible();

    // Execute re-dispatches the job; the list refetches and the status advances.
    await page.getByRole("button", { name: "Execute" }).click();
    await expect(page.getByRole("button", { name: "Execute" })).toBeHidden({ timeout: 15_000 });
    await expect(page.getByRole("cell", { name: "Completed" }).first()).toBeVisible();
  });
});
