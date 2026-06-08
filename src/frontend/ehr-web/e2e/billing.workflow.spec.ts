import { test, expect, mockAuth, stubRealtime, stubApiCatchAll, gotoWorkflow } from "./fixtures";

/**
 * Billing workflow walkthrough (recorded to video): the operator opens the dialysis charges & claims
 * board, sees captured charges and the claim they rolled into, then drills into the claim's
 * clearinghouse acknowledgement timeline (999 / 277CA). Backend fully mocked.
 */

const CLAIM_ID = "9f3a1b2c-0000-4000-8000-claim000001";

const charge = {
  chargeId: "1a2b3c4d-0000-4000-8000-charge00001",
  patientId: "22222222-2222-2222-2222-222222222222",
  encounterId: "33333333-3333-3333-3333-333333333333",
  cptCode: "90937",
  billedAmount: 447.0,
  currencyCode: "USD",
  status: "OnClaim",
  assignedClaimId: CLAIM_ID,
  diagnosisPointerIcd10Codes: ["N18.6", "E11.9"],
};

const claim = {
  claimId: CLAIM_ID,
  patientId: "22222222-2222-2222-2222-222222222222",
  payerId: "44444444-4444-4444-4444-444444444444",
  payerCode: "MEDICARE",
  claimFormatCode: "837P",
  billedTotal: 447.0,
  currencyCode: "USD",
  status: "Acknowledged",
  externalControlNumber: "CN-2026-0042",
  payerClaimControlNumber: "PCN-778812",
  submittedAtUtc: "2026-06-08T14:30:00Z",
  acknowledgedAtUtc: "2026-06-08T15:02:00Z",
  chargeCount: 1,
  acknowledgementCount: 2,
};

const acks = {
  claimId: CLAIM_ID,
  status: "Acknowledged",
  externalControlNumber: "CN-2026-0042",
  payerClaimControlNumber: "PCN-778812",
  acknowledgedAtUtc: "2026-06-08T15:02:00Z",
  acknowledgements: [
    {
      acknowledgementId: "ack-1",
      kind: "999",
      verdict: "Accepted",
      payerClaimControlNumber: null,
      reasonCodes: [],
      receivedAtUtc: "2026-06-08T14:45:00Z",
    },
    {
      acknowledgementId: "ack-2",
      kind: "277CA",
      verdict: "Accepted",
      payerClaimControlNumber: "PCN-778812",
      reasonCodes: [],
      receivedAtUtc: "2026-06-08T15:02:00Z",
    },
  ],
};

test.describe("ehr-web — billing charges & claims workflow", () => {
  test("charges → claim → acknowledgement timeline", async ({ page }) => {
    await stubRealtime(page);
    await mockAuth(page, { permissions: ["ehr.billing.read"] });
    await stubApiCatchAll(page);

    await page.route("**/ehr/api/v1.0/billing/**", async (route) => {
      const path = new URL(route.request().url()).pathname;
      if (path.endsWith("/billing/charges")) return route.fulfill({ json: [charge] });
      if (/\/billing\/claims\/[^/]+\/acks$/.test(path)) return route.fulfill({ json: acks });
      if (path.endsWith("/billing/claims")) return route.fulfill({ json: [claim] });
      return route.fallback();
    });

    await gotoWorkflow(page, "/ehr/admin/billing/dialysis-charges", "Dialysis charges & claims");

    // The captured charge and the claim it rolled into are both listed.
    await expect(page.getByText("90937")).toBeVisible();
    await expect(page.getByText("MEDICARE")).toBeVisible();

    // Drill into the claim's clearinghouse acknowledgement timeline.
    await page.getByRole("button", { name: "Acks" }).first().click();
    const drawer = page.getByRole("dialog");
    await expect(drawer.getByRole("heading", { name: "Ack timeline" })).toBeVisible();
    await expect(drawer.getByText("277CA")).toBeVisible();
    await expect(drawer.getByText("Accepted").first()).toBeVisible();
  });
});
