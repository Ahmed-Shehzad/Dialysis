import { lazy, Suspense, useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  abortSession,
  completeSession,
  pauseSession,
  resumeSession,
  startSession,
  type DialysisSessionSummary,
} from "../api/sessionsApi";
import { fetchDocuments } from "@/features/documents/api/documentsApi";
import { notify } from "@/features/durable-commands";

// Lazy so the heavy react-pdf bundle only downloads when the user opens the invoice.
const PdfViewerDrawer = lazy(() =>
  import("@/features/documents/components/PdfViewerDrawer").then((m) => ({
    default: m.PdfViewerDrawer,
  })),
);

export type SessionLifecycleControlsProps = {
  session: DialysisSessionSummary | undefined;
};

const buttonClass =
  "rounded-md px-3 py-1.5 text-sm font-medium transition disabled:opacity-40 disabled:cursor-not-allowed";

// How long to poll HIE for the generated invoice before giving up (it arrives via an async
// event chain: PDMS → EHR billing → HIE document render).
const INVOICE_WATCH_TIMEOUT_MS = 60_000;

export const SessionLifecycleControls = ({ session }: SessionLifecycleControlsProps) => {
  const queryClient = useQueryClient();
  const [achieved, setAchieved] = useState(2.5);
  const [reason, setReason] = useState("MEDICAL");

  const [watchInvoice, setWatchInvoice] = useState(false);
  const [invoiceDocId, setInvoiceDocId] = useState<string | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const watchStartedAt = useRef<number>(0);

  const patientId = session?.patientId;

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["pdms", "sessions", "active"] });
    queryClient.invalidateQueries({ queryKey: ["sessions", session?.id, "readings"] });
  };

  const startMutation = useMutation({
    mutationFn: () => startSession(session!.id),
    onSuccess: invalidate,
  });
  const completeMutation = useMutation({
    mutationFn: () => completeSession(session!.id, achieved),
    onSuccess: () => {
      invalidate();
      // The invoice is rendered asynchronously downstream — start watching for it.
      setInvoiceDocId(null);
      watchStartedAt.current = Date.now();
      setWatchInvoice(true);
      notify({ kind: "info", message: "Session completed — generating invoice…" });
    },
  });
  const abortMutation = useMutation({
    mutationFn: () => abortSession(session!.id, reason),
    onSuccess: invalidate,
  });
  const pauseMutation = useMutation({
    mutationFn: () => pauseSession(session!.id),
    onSuccess: invalidate,
  });
  const resumeMutation = useMutation({
    mutationFn: () => resumeSession(session!.id),
    onSuccess: invalidate,
  });

  // Poll HIE for the freshly generated invoice while watching.
  const invoiceQuery = useQuery({
    queryKey: ["hie", "documents", "invoice", patientId],
    queryFn: () => fetchDocuments({ patientId: patientId!, kind: "invoice" }),
    enabled: watchInvoice && Boolean(patientId),
    refetchInterval: watchInvoice ? 3_000 : false,
  });

  useEffect(() => {
    if (!watchInvoice) return;
    const rows = invoiceQuery.data ?? [];
    // Only accept an invoice created since we hit Complete (avoids surfacing a prior session's).
    const newest = rows
      .filter((r) => new Date(r.createdAtUtc).getTime() >= watchStartedAt.current - 5_000)
      .sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime())[0];
    if (newest) {
      setInvoiceDocId(newest.id);
      setWatchInvoice(false);
      notify({ kind: "success", message: `Invoice ready — ${newest.title}` });
      return;
    }
    if (Date.now() - watchStartedAt.current > INVOICE_WATCH_TIMEOUT_MS) {
      setWatchInvoice(false);
      notify({
        kind: "error",
        message: "Invoice is taking longer than expected — check Documents.",
      });
    }
  }, [invoiceQuery.data, watchInvoice]);

  if (!session) return null;

  const canStart = session.status === "Scheduled";
  const canFinish = session.status === "InProgress";
  const isPaused = session.status === "Paused";
  // A paused session can still be aborted; completion requires it to be running.
  const canAbort = canFinish || isPaused;

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <h3 className="mb-3 text-sm font-medium text-slate-200">Session lifecycle</h3>
      <div className="flex flex-wrap items-center gap-4">
        <button
          type="button"
          onClick={() => startMutation.mutate()}
          disabled={!canStart || startMutation.isPending}
          className={`${buttonClass} bg-clinic-600 text-white hover:bg-clinic-700`}
        >
          {startMutation.isPending ? "Starting…" : "Start"}
        </button>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => completeMutation.mutate()}
            disabled={!canFinish || completeMutation.isPending}
            className={`${buttonClass} bg-emerald-600 text-white hover:bg-emerald-700`}
          >
            {completeMutation.isPending ? "Completing…" : "Complete"}
          </button>
          <label className="text-xs text-slate-400">
            <span>UF L</span>
            <input
              type="number"
              step="0.1"
              min={0}
              value={achieved}
              onChange={(e) => setAchieved(Number(e.target.value))}
              className="ml-2 w-20 rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-sm text-slate-100"
              disabled={!canFinish}
            />
          </label>
        </div>

        {isPaused ? (
          <button
            type="button"
            onClick={() => resumeMutation.mutate()}
            disabled={resumeMutation.isPending}
            className={`${buttonClass} bg-amber-600 text-white hover:bg-amber-700`}
          >
            {resumeMutation.isPending ? "Resuming…" : "Resume"}
          </button>
        ) : (
          <button
            type="button"
            onClick={() => pauseMutation.mutate()}
            disabled={!canFinish || pauseMutation.isPending}
            className={`${buttonClass} bg-amber-600 text-white hover:bg-amber-700`}
          >
            {pauseMutation.isPending ? "Pausing…" : "Pause"}
          </button>
        )}

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => abortMutation.mutate()}
            disabled={!canAbort || abortMutation.isPending}
            className={`${buttonClass} bg-rose-600 text-white hover:bg-rose-700`}
          >
            {abortMutation.isPending ? "Aborting…" : "Abort"}
          </button>
          <select
            aria-label="Abort reason"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-sm text-slate-100"
            disabled={!canAbort}
          >
            <option value="MEDICAL">Medical</option>
            <option value="MACHINE">Machine</option>
            <option value="PATIENT_REQUEST">Patient request</option>
            <option value="OTHER">Other</option>
          </select>
        </div>

        {watchInvoice && <span className="text-xs text-slate-400">Generating invoice…</span>}
        {invoiceDocId && (
          <button
            type="button"
            onClick={() => setDrawerOpen(true)}
            className={`${buttonClass} bg-sky-600 text-white hover:bg-sky-500`}
          >
            View invoice
          </button>
        )}

        <div className="ml-auto text-xs text-slate-400">
          Current: <span className="font-mono">{session.status}</span>
        </div>
      </div>
      {(startMutation.error || completeMutation.error || abortMutation.error) && (
        <div className="mt-2 text-xs text-rose-300">
          Action failed — the server rejected the state transition.
        </div>
      )}

      {drawerOpen && invoiceDocId && (
        <Suspense fallback={null}>
          <PdfViewerDrawer documentId={invoiceDocId} onClose={() => setDrawerOpen(false)} />
        </Suspense>
      )}
    </section>
  );
};
