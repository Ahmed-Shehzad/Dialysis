import { expect, test } from "@playwright/test";
import { signIn } from "../helpers/signIn";

// Golden-path "the page loads" coverage for every operator admin page shipped across PRs
// 9–12. Each case signs in once, navigates to the route, and asserts the page heading
// renders — i.e. the lazy chunk loads, the route resolves, and the first query fires without
// crashing. Round-trip mutations live in the per-page specs (inventory / reporting-templates /
// record-administration); this spec is the broad smoke net.
//
// Runs against the live Aspire stack (E2E_BASE_URL, default http://localhost:9090). The
// per-test timeout matches new-channel.spec.ts because the OIDC round-trip on a freshly-started
// stack can exceed 60s.
test.describe.configure({ timeout: 180_000 });

const PAGES: Array<{ route: string; heading: RegExp }> = [
  // PR 9 — PDMS OnCall.
  { route: "/admin/oncall/rotation", heading: /On-call rotation/i },
  { route: "/admin/oncall/policies", heading: /Escalation policy/i },
  { route: "/admin/oncall/audit", heading: /Alarm dispatch audit/i },
  // PR 10 — Billing + DataProtection.
  { route: "/admin/billing/dialysis-charges", heading: /Dialysis charges & claims/i },
  { route: "/admin/billing/fee-schedule", heading: /CPT fee schedule/i },
  { route: "/admin/billing/exports", heading: /Billing export jobs/i },
  { route: "/admin/data-protection/ropa", heading: /Records of Processing Activities/i },
  { route: "/admin/data-protection/consents", heading: /Patient consents/i },
  { route: "/admin/data-protection/data-subject-rights", heading: /Data subject rights/i },
  // Existing admin surfaces these specs also guard.
  { route: "/admin/inventory", heading: /Medication inventory/i },
  { route: "/admin/reporting/templates", heading: /Reporting templates/i },
  // PR — Document retention + DSR Art. 17 erasure pipeline.
  { route: "/hie/admin/documents/retention", heading: /Document retention/i },
  // PR — TEFCA QHIN onboarding.
  { route: "/hie/admin/tefca/partners", heading: /TEFCA QHIN partners/i },
];

test("every operator admin page loads its heading after sign-in", async ({ page }) => {
  await signIn(page);

  for (const { route, heading } of PAGES) {
    await page.goto(route);
    await expect(page.getByRole("heading", { name: heading }).first()).toBeVisible({
      timeout: 30_000,
    });
  }
});
