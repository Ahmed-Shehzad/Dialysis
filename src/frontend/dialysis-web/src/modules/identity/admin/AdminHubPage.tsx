import { Link } from "react-router-dom";
import { ModuleHeader } from "@/shell/ModuleHeader";

type AdminLink = {
  /** Button label — mirrors the destination page's heading so the two read the same. */
  label: string;
  /** Absolute route, matching the owning module manifest's `renderRoutes()`. */
  to: string;
  /** One-line "what's here" so an operator can pick without opening every page. */
  description: string;
};

type AdminGroup = {
  /** Operational area heading. */
  title: string;
  /** Backend module(s) that own the data, shown as a small tag for orientation. */
  owner: string;
  links: AdminLink[];
};

// Curated catalog of every admin surface in the SPA, grouped by operational area. The route
// strings mirror each module manifest's `renderRoutes()` (HIS / EHR / PDMS / HIE / Identity);
// this hub is the single place an administrator discovers and jumps between them instead of
// typing URLs. Adding a new admin page = add its module Route + one entry here.
const ADMIN_GROUPS: readonly AdminGroup[] = [
  {
    title: "Billing & claims",
    owner: "HIS · EHR",
    links: [
      {
        label: "Billing export jobs",
        to: "/admin/billing/exports",
        description: "Track and re-run claim export batches.",
      },
      {
        label: "Dialysis charges & claims",
        to: "/admin/billing/dialysis-charges",
        description: "Review per-session charges and claim status.",
      },
      {
        label: "CPT fee schedule",
        to: "/admin/billing/fee-schedule",
        description: "Maintain CPT procedure rates.",
      },
    ],
  },
  {
    title: "Inventory & supplies",
    owner: "PDMS",
    links: [
      {
        label: "Medication inventory",
        to: "/admin/inventory",
        description: "Stock levels, lots, and reorder points.",
      },
    ],
  },
  {
    title: "Reporting",
    owner: "PDMS",
    links: [
      {
        label: "Reporting templates",
        to: "/admin/reporting/templates",
        description: "Author and publish operational report templates.",
      },
    ],
  },
  {
    title: "On-call & escalation",
    owner: "PDMS",
    links: [
      {
        label: "On-call rotation",
        to: "/admin/oncall/rotation",
        description: "Who's on call and when.",
      },
      {
        label: "Escalation policy",
        to: "/admin/oncall/policies",
        description: "Alarm escalation rules and thresholds.",
      },
      {
        label: "Alarm dispatch audit",
        to: "/admin/oncall/audit",
        description: "Trace of every alarm dispatch.",
      },
    ],
  },
  {
    title: "Documents & exchange",
    owner: "HIE",
    links: [
      {
        label: "Documents",
        to: "/admin/documents",
        description: "Browse and sign exchanged clinical documents.",
      },
      {
        label: "Document retention",
        to: "/admin/documents/retention",
        description: "Storage-limitation purge policies (GDPR Art. 5).",
      },
      {
        label: "TEFCA QHIN partners",
        to: "/admin/tefca/partners",
        description: "Onboard and manage exchange partners.",
      },
    ],
  },
  {
    title: "Identity & compliance",
    owner: "Identity",
    links: [
      {
        label: "Identity & claims",
        to: "/admin/identity",
        description: "Signed-in user's roles, claims, and access token.",
      },
      {
        label: "HIPAA dashboard",
        to: "/admin/hipaa",
        description: "Federated safeguard health-check across modules.",
      },
      {
        label: "Records of Processing (RoPA)",
        to: "/admin/data-protection/ropa",
        description: "GDPR Art. 30 processing-activity register.",
      },
      {
        label: "Patient consents",
        to: "/admin/data-protection/consents",
        description: "Consent records and lawful bases.",
      },
      {
        label: "Data subject rights",
        to: "/admin/data-protection/data-subject-rights",
        description: "Art. 15 export & Art. 17 erasure requests.",
      },
    ],
  },
] as const;

/**
 * Admin hub — the front door for every operator/administrator surface in the SPA. Each
 * card is a navigation button into a module's admin page (billing, inventory, reporting,
 * on-call, documents, identity/compliance). Before this page those routes were reachable
 * only by typing the URL; the top-nav "Admin" entry now lands here.
 */
export const AdminHubPage = () => (
  <div className="space-y-6">
    <ModuleHeader moduleSlug="identity" />

    {ADMIN_GROUPS.map((group) => (
      <section
        key={group.title}
        className="space-y-3"
        aria-labelledby={`admin-group-${group.title}`}
      >
        <div className="flex items-baseline justify-between">
          <h2
            id={`admin-group-${group.title}`}
            className="text-sm font-semibold uppercase tracking-wider text-slate-300"
          >
            {group.title}
          </h2>
          <span className="text-xs text-slate-500">{group.owner}</span>
        </div>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {group.links.map((link) => (
            <Link
              key={link.to}
              to={link.to}
              className="group rounded-lg border border-slate-800 bg-slate-900/40 p-4 transition hover:border-clinic-600 hover:bg-slate-900/70"
            >
              <span className="block text-sm font-medium text-clinic-50 group-hover:text-clinic-100">
                {link.label}
              </span>
              <span className="mt-1 block text-xs text-slate-400">{link.description}</span>
            </Link>
          ))}
        </div>
      </section>
    ))}
  </div>
);
