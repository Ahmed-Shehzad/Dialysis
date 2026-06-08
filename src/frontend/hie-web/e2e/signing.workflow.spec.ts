import {
  test,
  expect,
  mockAuth,
  stubRealtime,
  stubApiCatchAll,
  gotoWorkflow,
  installDocumentRoutes,
} from "./fixtures";

/**
 * PDF signing workflow walkthrough (recorded to video): the operator opens a branded document with
 * no signatures yet, picks the certificate source + PAdES conformance level, applies the signature
 * (POST …/sign), and the signature history updates live with the new PAdES-LT row. Backend mocked;
 * the served PDF is the real branded fixture so the video shows the corporate template being signed.
 */

const DOC_ID = "doc-discharge-9001";

const documentRow = {
  id: DOC_ID,
  patientId: "22222222-2222-2222-2222-222222222222",
  kind: "invoice",
  title: "Invoice INV-2026-0042",
  mimeType: "application/pdf",
  languageCode: "en-US",
  status: "Current",
  source: "Billing",
  size: 43726,
  createdAtUtc: "2026-06-08T10:00:00Z",
  signatureCount: 0,
  hasAcroForms: true,
  hasJavascript: false,
  category: "11111111",
};

const documentDetail = (signed: boolean) => ({
  ...documentRow,
  signatureCount: signed ? 1 : 0,
  createdBy: "demo-staff",
  contentHash: "f1d2c3b4a5e6f7081920",
  allowJavaScriptExecution: false,
  signatures: signed
    ? [
        {
          id: "sig-1",
          signerKind: "Platform",
          certThumbprint: "AB12CD34EF560718293A",
          signedAtUtc: "2026-06-08T10:05:00Z",
          reason: "Issued for submission",
          padesLevel: "LT",
          signatureFormat: "Aes",
          tsaUri: "http://tsa.example/ts",
          timestampedAtUtc: "2026-06-08T10:05:01Z",
          revocationEvidenceFormat: "Both",
        },
      ]
    : [],
});

test.describe("hie-web — PDF signing workflow", () => {
  test("open → choose cert + PAdES level → sign → signature history", async ({ page }) => {
    await stubRealtime(page);
    await mockAuth(page, {
      permissions: ["hie.documents.list", "hie.documents.read", "hie.documents.sign"],
    });
    await stubApiCatchAll(page);
    await installDocumentRoutes(page, {
      id: DOC_ID,
      rows: [documentRow],
      detail: documentDetail,
      pdfFixture: "invoice-acroform.pdf",
    });

    await gotoWorkflow(page, "/hie/admin/documents", "Documents");
    await page.getByRole("button", { name: "Open" }).first().click();

    const drawer = page.getByRole("dialog");
    await expect(drawer.getByText("No signatures yet.")).toBeVisible();

    // Choose the certificate source and PAdES conformance level, with a reason.
    await drawer.getByLabel("Cert source").selectOption("Platform");
    await drawer.getByLabel("PAdES level").selectOption("LT");
    await drawer.getByLabel("Reason (optional)").fill("Issued for submission");

    // Apply the signature (mocked). The detail refetch returns the new signature row.
    await drawer.getByRole("button", { name: "Sign document" }).click();

    await expect(drawer.getByText("No signatures yet.")).toBeHidden();
    await expect(drawer.getByText(/PAdES-LT/)).toBeVisible({ timeout: 15_000 });
  });
});
