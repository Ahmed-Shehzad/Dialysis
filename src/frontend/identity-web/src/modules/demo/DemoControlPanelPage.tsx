import { lazy, Suspense, useMemo, useState } from "react";
import { useNavigate } from "react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  completeSession,
  fetchActiveSessions,
  pauseSession,
  resumeSession,
  type DialysisSessionSummary,
} from "@/features/sessions/api/sessionsApi";
import { fetchDocuments } from "@/features/documents/api/documentsApi";
import { resetDemoSessions } from "@/features/demo/api/demoApi";
import { notify } from "@/features/durable-commands";
import { usePatientContext } from "@/shell/PatientContextProvider";

const PdfViewerDrawer = lazy(() =>
  import("@/features/documents/components/PdfViewerDrawer").then((m) => ({
    default: m.PdfViewerDrawer,
  })),
);

const btn =
  "rounded-md px-3 py-2 text-sm font-medium transition disabled:opacity-40 disabled:cursor-not-allowed";

type WalkStep = {
  module: string;
  title: string;
  body: string;
  cta: string;
  /** Cross-context destination (another `/{context}` app) — a full-page hop. */
  href?: string;
  /** In-app destination within this (Identity, `/admin`) app's router. */
  to?: string;
};

// The end-to-end patient journey, in the order a presenter should click through it. Each step
// names the module that owns the screen so the audience can map UI → architecture. Every screen
// except the final Identity hub lives in another `/{context}` app, so those steps are full-page
// hops (`href`); the Identity hub is in-app (`to`).
const WALKTHROUGH: readonly WalkStep[] = [
  {
    module: "HIS",
    title: "1 · Front desk: admit, queue, assign a chair",
    body: "The receptionist's Today board shows scheduled arrivals, walk-ins, check-in, and chair assignment. The selected patient then follows you across every module.",
    cta: "Open HIS Today",
    href: "/his/today",
  },
  {
    module: "EHR",
    title: "2 · Clinical chart: problems, allergies, meds, vitals",
    body: "Open a patient's chart to show the longitudinal record — diagnoses, allergies, medications, and observations seeded for each demo patient.",
    cta: "Open patients",
    href: "/ehr/patients",
  },
  {
    module: "PDMS",
    title: "3 · Chairside: live vitals + live treatment cost",
    body: "The chairside monitor streams vitals and a running, itemised treatment cost over SignalR while the machine usage timer ticks. Use the control panel above to jump into a live session.",
    cta: "Open sessions",
    href: "/pdms/sessions",
  },
  {
    module: "HIS · PDMS",
    title: "4 · Chair board + pause-aware accounting",
    body: "The chair board shows live occupancy (HIS placements broadcast cross-module to PDMS). Then use the control panel's Pause/Resume: the usage timer and live cost freeze while paused (machine off) and resume excludes the paused span, so billing reflects true machine on-time.",
    cta: "Open chair board",
    href: "/pdms/chairs",
  },
  {
    module: "EHR · HIE",
    title: "5 · Complete → charge → invoice → document",
    body: "Completing a session prices the charge (EHR), renders an AcroForm invoice PDF (HIE), and indexes it as a clinical document. The autopilot produces these continuously.",
    cta: "Open Admin · Documents",
    href: "/hie/admin/documents",
  },
  {
    module: "SmartConnect",
    title: "6 · Integration engine: live HL7 v2 feeds",
    body: "ADT and ORU messages flow through the channel flows on a timer, showing the interoperability layer that bridges legacy systems into FHIR.",
    cta: "Open integrations",
    href: "/smartconnect/integrations",
  },
  {
    module: "HIE",
    title: "7 · Exchange: FHIR partners + consent",
    body: "Send a FHIR Bundle to a partner organisation and review the consent policies that gate what's shared across organisations.",
    cta: "Open FHIR exchange",
    href: "/hie/fhir-exchange",
  },
  {
    module: "Identity",
    title: "8 · Admin & compliance: HIPAA, RoPA, DSR",
    body: "The administrator console: identity claims, the federated HIPAA safeguard dashboard, GDPR records-of-processing, and data-subject-rights workflows.",
    cta: "Open Admin hub",
    to: "/",
  },
];

export const DemoControlPanelPage = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { select } = usePatientContext();
  const [invoiceOpen, setInvoiceOpen] = useState(false);

  const sessionsQuery = useQuery({
    queryKey: ["pdms", "sessions", "list", "recent"],
    queryFn: () => fetchActiveSessions(false),
    refetchInterval: 5_000,
  });
  const sessions = sessionsQuery.data ?? [];
  const live = sessions.find((s) => s.status === "InProgress");
  const paused = sessions.find((s) => s.status === "Paused");
  const scheduled = sessions.find((s) => s.status === "Scheduled");

  const invoiceQuery = useQuery({
    queryKey: ["hie", "documents", "invoice", "latest"],
    queryFn: () => fetchDocuments({ kind: "invoice" }),
    refetchInterval: 5_000,
  });
  const latestInvoice = useMemo(
    () =>
      [...(invoiceQuery.data ?? [])].sort(
        (a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime(),
      )[0],
    [invoiceQuery.data],
  );

  const invalidateSessions = () =>
    queryClient.invalidateQueries({ queryKey: ["pdms", "sessions"] });

  const resetMutation = useMutation({
    mutationFn: resetDemoSessions,
    onSuccess: (r) => {
      invalidateSessions();
      notify({ kind: "success", message: `Demo reset — ${r.sessions} sessions reseeded.` });
    },
    onError: () => notify({ kind: "error", message: "Reset failed — is the demo enabled?" }),
  });
  const pauseMutation = useMutation({
    mutationFn: (id: string) => pauseSession(id),
    onSuccess: invalidateSessions,
  });
  const resumeMutation = useMutation({
    mutationFn: (id: string) => resumeSession(id),
    onSuccess: invalidateSessions,
  });
  const completeMutation = useMutation({
    mutationFn: (id: string) => completeSession(id, 2.4),
    onSuccess: () => {
      invalidateSessions();
      notify({ kind: "info", message: "Completing session — invoice generating downstream…" });
    },
  });

  const openSession = (session: DialysisSessionSummary | undefined, hint: string) => {
    if (!session) {
      notify({ kind: "info", message: `${hint} — try "Reset & reseed" first.` });
      return;
    }
    select({
      id: session.patientId,
      displayName: `Patient ${session.patientId.slice(0, 8)}…`,
    });
    // The chairside session lives in the PDMS app — a full-page hop (the shared-origin patient
    // context carries the selection across).
    globalThis.location.assign(`/pdms/sessions/${session.id}`);
  };

  const viewLatestInvoice = () => {
    if (latestInvoice) {
      setInvoiceOpen(true);
    } else {
      notify({
        kind: "info",
        message: "No invoice yet — autopilot makes one ~a minute after start.",
      });
      // Documents live in the HIE app — a full-page hop.
      globalThis.location.assign("/hie/admin/documents");
    }
  };

  return (
    <div className="space-y-8">
      <header>
        <h1 className="text-xl font-semibold text-slate-100">Demo control panel</h1>
        <p className="mt-1 text-sm text-slate-400">
          One place to drive the MVP demo: reset to a clean snapshot, jump into a live session,
          drive the lifecycle, and open the generated invoice — plus a guided walkthrough of every
          process in the platform. An autopilot continuously completes sessions so invoices and
          documents keep appearing.
        </p>
      </header>

      <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wider text-slate-300">
          Control panel
        </h2>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => resetMutation.mutate()}
            disabled={resetMutation.isPending}
            className={`${btn} bg-rose-600 text-white hover:bg-rose-500`}
          >
            {resetMutation.isPending ? "Resetting…" : "Reset & reseed"}
          </button>
          <button
            type="button"
            onClick={() => openSession(live, "No in-progress session")}
            className={`${btn} bg-clinic-600 text-white hover:bg-clinic-700`}
          >
            Open live session
          </button>
          <button
            type="button"
            onClick={() => openSession(scheduled, "No scheduled session")}
            className={`${btn} bg-sky-700 text-white hover:bg-sky-600`}
          >
            Open scheduled session
          </button>
          <button
            type="button"
            onClick={() => live && pauseMutation.mutate(live.id)}
            disabled={!live || pauseMutation.isPending}
            className={`${btn} bg-amber-600 text-white hover:bg-amber-700`}
          >
            Pause live
          </button>
          <button
            type="button"
            onClick={() => paused && resumeMutation.mutate(paused.id)}
            disabled={!paused || resumeMutation.isPending}
            className={`${btn} bg-amber-600 text-white hover:bg-amber-700`}
          >
            Resume paused
          </button>
          <button
            type="button"
            onClick={() => live && completeMutation.mutate(live.id)}
            disabled={!live || completeMutation.isPending}
            className={`${btn} bg-emerald-600 text-white hover:bg-emerald-700`}
          >
            Complete live → invoice
          </button>
          <button
            type="button"
            onClick={viewLatestInvoice}
            className={`${btn} bg-slate-700 text-slate-100 hover:bg-slate-600`}
          >
            View latest invoice
          </button>
          <button
            type="button"
            onClick={() => globalThis.location.assign("/hie/admin/documents")}
            className={`${btn} bg-slate-700 text-slate-100 hover:bg-slate-600`}
          >
            Admin · Documents
          </button>
        </div>
        <p className="mt-3 text-xs text-slate-500">
          Sessions now: {sessions.length} total ·{" "}
          {sessions.filter((s) => s.status === "InProgress").length} in progress ·{" "}
          {sessions.filter((s) => s.status === "Paused").length} paused ·{" "}
          {sessions.filter((s) => s.status === "Scheduled").length} scheduled. Latest invoice:{" "}
          {latestInvoice ? latestInvoice.title : "—"}.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold uppercase tracking-wider text-slate-300">
          Guided walkthrough
        </h2>
        <ol className="space-y-3">
          {WALKTHROUGH.map((step) => (
            <li
              key={step.title}
              className="flex flex-col gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4 sm:flex-row sm:items-center sm:justify-between"
            >
              <div className="min-w-0">
                <div className="flex items-center gap-2">
                  <span className="rounded bg-slate-800 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-slate-300">
                    {step.module}
                  </span>
                  <span className="text-sm font-medium text-clinic-50">{step.title}</span>
                </div>
                <p className="mt-1 text-xs text-slate-400">{step.body}</p>
              </div>
              <button
                type="button"
                onClick={() =>
                  step.href ? globalThis.location.assign(step.href) : navigate(step.to ?? "/")
                }
                className={`${btn} shrink-0 bg-clinic-600 text-white hover:bg-clinic-700`}
              >
                {step.cta}
              </button>
            </li>
          ))}
        </ol>
      </section>

      {invoiceOpen && latestInvoice && (
        <Suspense fallback={null}>
          <PdfViewerDrawer documentId={latestInvoice.id} onClose={() => setInvoiceOpen(false)} />
        </Suspense>
      )}
    </div>
  );
};

export default DemoControlPanelPage;
