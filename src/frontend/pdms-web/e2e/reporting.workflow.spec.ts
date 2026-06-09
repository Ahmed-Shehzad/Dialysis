import {
  test,
  expect,
  mockAuth,
  stubRealtime,
  stubApiCatchAll,
  fixturePdf,
  gotoSessionDocuments,
} from "./fixtures";

/**
 * Reporting workflow walkthrough (recorded to video): the operator opens a completed session's
 * Documents tab, sees the generated PDMS reports (discharge letter, billing summary) and the branded
 * invoice, then opens the invoice in the viewer — pdfjs renders the real branded PDF. Backend fully
 * mocked; the served PDF is the BrandedPdfFixtureGenerator output so the video shows the new template.
 */

const SESSION_ID = "11111111-1111-1111-1111-111111111111";
const PATIENT_ID = "22222222-2222-2222-2222-222222222222";
const INVOICE_ID = "inv-2026-0042";

const session = {
  id: SESSION_ID,
  patientId: PATIENT_ID,
  status: "Completed",
  scheduledStartUtc: "2026-06-08T08:00:00Z",
  actualStartUtc: "2026-06-08T08:00:00Z",
  actualEndUtc: "2026-06-08T12:00:00Z",
  machineId: "machine-b12",
  pausedAtUtc: null,
  accumulatedPausedSeconds: 0,
};

const reports = [
  {
    id: "rep-discharge",
    sessionId: SESSION_ID,
    patientId: PATIENT_ID,
    kind: "DischargeLetter",
    status: "Generated",
    format: "application/pdf",
    contentHash: "f1d2c3b4a5e6f7081920",
    storageRef: "blob://reports/rep-discharge",
    generatedAtUtc: "2026-06-08T12:01:00Z",
    deliveredAtUtc: null,
    failureReason: null,
  },
  {
    id: "rep-billing",
    sessionId: SESSION_ID,
    patientId: PATIENT_ID,
    kind: "BillingDocument",
    status: "Generated",
    format: "application/pdf",
    contentHash: "0a1b2c3d4e5f60718293",
    storageRef: "blob://reports/rep-billing",
    generatedAtUtc: "2026-06-08T12:01:05Z",
    deliveredAtUtc: null,
    failureReason: null,
  },
];

const invoiceRow = {
  id: INVOICE_ID,
  patientId: PATIENT_ID,
  kind: "invoice",
  title: "Invoice INV-2026-0042",
  mimeType: "application/pdf",
  status: "Current",
  source: "Billing",
  size: 43726,
  createdAtUtc: "2026-06-08T12:02:00Z",
  signatureCount: 0,
  hasAcroForms: true,
  hasJavascript: false,
  category: SESSION_ID,
};

const invoiceDetail = {
  ...invoiceRow,
  createdBy: "demo-staff",
  contentHash: "f1d2c3b4a5e6f7081920",
  allowJavaScriptExecution: false,
  signatures: [],
};

test.describe("pdms-web — session reporting workflow", () => {
  test("session documents → reports + invoice → open viewer", async ({ page }) => {
    await stubRealtime(page);
    await mockAuth(page, { permissions: ["pdms.reports.read"] });
    await stubApiCatchAll(page);

    const pdfBytes = fixturePdf("invoice-acroform.pdf");
    await page.route("**/pdms/api/**", async (route) => {
      const request = route.request();
      const path = new URL(request.url()).pathname;
      if (request.method() !== "GET") return route.fallback();
      if (path.endsWith("/api/v1.0/sessions")) return route.fulfill({ json: [session] });
      if (/\/sessions\/[^/]+\/reports$/.test(path)) return route.fulfill({ json: reports });
      if (path.includes("/_x/hie/api/v1.0/documents")) {
        if (path.endsWith("/binary")) {
          return route.fulfill({ contentType: "application/pdf", body: pdfBytes });
        }
        if (path.endsWith("/preview")) {
          return route.fulfill({ json: { data: { format: "Pdf", mimeType: "application/pdf" } } });
        }
        if (path.endsWith("/documents")) return route.fulfill({ json: { data: [invoiceRow] } });
        return route.fulfill({ json: { data: invoiceDetail } });
      }
      return route.fallback();
    });

    await gotoSessionDocuments(page, SESSION_ID);

    // The generated PDMS reports and the branded invoice are listed for the session.
    await expect(page.getByText("Discharge letter")).toBeVisible();
    await expect(page.getByText("Billing summary")).toBeVisible();
    await expect(page.getByText("Invoice INV-2026-0042")).toBeVisible();

    // Open the invoice in the viewer — pdfjs renders the real branded PDF.
    await page.getByRole("button", { name: "Preview / edit" }).click();
    const drawer = page.getByRole("dialog");
    await expect(drawer.getByRole("heading", { name: "Invoice INV-2026-0042" })).toBeVisible({
      timeout: 15_000,
    });
  });
});
