import { expect, test } from "@playwright/test";
import { signIn } from "../helpers/signIn";
import { minimalPdfUpload } from "../helpers/minimalPdf";
import { selectAnyPatient } from "../helpers/selectPatient";

// Documents signing flow (PR #129) — exercises the platform-cert PAdES-B path end-to-end.
//
// This is opt-in: a real signing certificate must be configured via
//   Documents:Signing:PlatformCertificate:PfxPath
//   Documents:Signing:PlatformCertificate:PfxPassword
// — without it the POST to /sign returns 500 and the test self-skips with a useful message.
// PAdES-T / -LT / -LTA additionally require Documents:Signing:Tsa:Uri; the QES variant
// requires Documents:Signing:Tsp:* — both stay deferred until a sandbox TSP is wired.
//
//   Sign in → pick a patient → upload minimal PDF → open drawer → pick "PAdES level: B"
//   to keep the cert chain happy → click Sign → assert a signature row appears.
test.describe.configure({ timeout: 180_000 });

const SIGNING_CONFIGURED = process.env.E2E_DOCUMENTS_SIGNING_ENABLED === "1";

test("platform-cert sign appends a signature row to the document", async ({ page }) => {
  test.skip(
    !SIGNING_CONFIGURED,
    "Documents:Signing:PlatformCertificate is not configured in this environment — " +
      "set E2E_DOCUMENTS_SIGNING_ENABLED=1 plus the host-side cert path/password to run.",
  );

  await signIn(page);
  await page.goto("/admin/documents");
  await expect(page.getByRole("heading", { name: /^Documents$/i })).toBeVisible({
    timeout: 30_000,
  });

  const patientDisplay = await selectAnyPatient(page).catch(() => null);
  test.skip(
    patientDisplay === null,
    "No patient seeded — signing spec requires a patient context. Seed one first.",
  );

  // Upload the fixture PDF.
  const upload = minimalPdfUpload();
  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/api/v1.0/documents") && r.request().method() === "POST",
    ),
    page.locator('input[type="file"]').setInputFiles(upload),
  ]);
  const row = page.getByRole("row", { name: new RegExp(upload.name) });
  await expect(row).toBeVisible({ timeout: 15_000 });

  // Open the drawer and sign at PAdES-B with the platform cert.
  await row.getByRole("button", { name: /^Open$/i }).click();
  await expect(page.getByRole("dialog")).toBeVisible();

  await page.getByRole("dialog").locator("select").nth(0).selectOption("Platform");
  await page.getByRole("dialog").locator("select").nth(1).selectOption("B");
  await page.getByPlaceholder(/optional/i).fill("E2E platform sign smoke");

  await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes("/sign") && r.request().method() === "POST" && r.status() < 400,
    ),
    page.getByRole("button", { name: /Sign document/i }).click(),
  ]);

  // The drawer re-queries the detail endpoint after invalidation; a signature row appears.
  await expect(page.getByText(/PAdES-B/i).first()).toBeVisible({ timeout: 15_000 });
  await expect(page.getByText(/Aes/i).first()).toBeVisible();
  await expect(page.getByText(/Platform/i).first()).toBeVisible();
});
