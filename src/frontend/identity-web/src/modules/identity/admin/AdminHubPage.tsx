import { Link } from "react-router";
import { ModuleHeader } from "@/shell/ModuleHeader";

type AdminLink = {
  /** Button label — mirrors the destination page's heading so the two read the same. */
  label: string;
  /** In-app route within this (Identity, `/admin`) app's router. Mutually exclusive with `href`. */
  to?: string;
  /** Cross-context full-page URL to another `/{context}` app (e.g. `/hie/admin/documents`).
   * Rendered as a plain `<a>` so the browser loads that SPA — a React Router `<Link>` would
   * resolve it inside the `/admin` basename and 404. Mutually exclusive with `to`. */
  href?: string;
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

// Curated catalog of every admin surface in the SPA, grouped by operational area. Since the
// BFF-per-context split each module's admin pages live in their own app under a `/{context}`
// path, so links into another module are full-page hops (`href`) while Identity's own pages
// (this app, `/admin` basename) stay in-app (`to`). Adding a new admin page = add its module
// Route + one entry here.
const ADMIN_GROUPS: readonly AdminGroup[] = [
  {
    title: "Demo & walkthrough",
    owner: "All modules",
    links: [
      {
        label: "Demo control panel",
        to: "/demo",
        description:
          "Reset the demo, drive a live session, and run the guided cross-module walkthrough.",
      },
    ],
  },
  {
    title: "Billing & claims",
    owner: "HIS · EHR",
    links: [
      {
        label: "Billing export jobs",
        href: "/his/admin/billing/exports",
        description: "Track and re-run claim export batches.",
      },
      {
        label: "Dialysis charges & claims",
        href: "/ehr/admin/billing/dialysis-charges",
        description: "Review per-session charges and claim status.",
      },
      {
        label: "CPT fee schedule",
        href: "/ehr/admin/billing/fee-schedule",
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
        href: "/pdms/admin/inventory",
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
        href: "/pdms/admin/reporting/templates",
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
        href: "/pdms/admin/oncall/rotation",
        description: "Who's on call and when.",
      },
      {
        label: "Escalation policy",
        href: "/pdms/admin/oncall/policies",
        description: "Alarm escalation rules and thresholds.",
      },
      {
        label: "Alarm dispatch audit",
        href: "/pdms/admin/oncall/audit",
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
        href: "/hie/admin/documents",
        description: "Browse and sign exchanged clinical documents.",
      },
      {
        label: "Document retention",
        href: "/hie/admin/documents/retention",
        description: "Storage-limitation purge policies (GDPR Art. 5).",
      },
      {
        label: "TEFCA QHIN partners",
        href: "/hie/admin/tefca/partners",
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
        to: "/identity",
        description: "Signed-in user's roles, claims, and access token.",
      },
      {
        label: "HIPAA dashboard",
        to: "/hipaa",
        description: "Federated safeguard health-check across modules.",
      },
      {
        label: "Records of Processing (RoPA)",
        to: "/data-protection/ropa",
        description: "GDPR Art. 30 processing-activity register.",
      },
      {
        label: "Patient consents",
        to: "/data-protection/consents",
        description: "Consent records and lawful bases.",
      },
      {
        label: "Data subject rights",
        to: "/data-protection/data-subject-rights",
        description: "Art. 15 export & Art. 17 erasure requests.",
      },
    ],
  },
] as const;

const cardClass =
  "group rounded-lg border border-slate-800 bg-slate-900/40 p-4 transition hover:border-clinic-600 hover:bg-slate-900/70";

const CardBody = ({ link }: { link: AdminLink }) => (
  <>
    <span className="block text-sm font-medium text-clinic-50 group-hover:text-clinic-100">
      {link.label}
    </span>
    <span className="mt-1 block text-xs text-slate-400">{link.description}</span>
  </>
);

/**
 * Admin hub — the front door for every operator/administrator surface in the SPA. Each
 * card jumps into a module's admin page (billing, inventory, reporting, on-call, documents,
 * identity/compliance). Cross-module pages live in other `/{context}` apps, so those cards
 * are full-page hops; Identity's own pages stay in-app.
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
          {group.links.map((link) =>
            link.href ? (
              <a key={link.href} href={link.href} aria-label={link.label} className={cardClass}>
                <CardBody link={link} />
              </a>
            ) : (
              <Link key={link.to} to={link.to ?? ""} aria-label={link.label} className={cardClass}>
                <CardBody link={link} />
              </Link>
            ),
          )}
        </div>
      </section>
    ))}
  </div>
);
