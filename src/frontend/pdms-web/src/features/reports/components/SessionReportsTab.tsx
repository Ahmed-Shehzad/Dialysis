import { lazy, Suspense, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  downloadReportBinary,
  fetchSessionReports,
  type SessionReport,
} from "@/features/reports/api/reportsApi";
import {
  downloadDocumentBinary,
  fetchDocuments,
  type DocumentRow,
} from "@/features/documents/api/documentsApi";

const PdfViewerDrawer = lazy(() =>
  import("@/features/documents/components/PdfViewerDrawer").then((m) => ({
    default: m.PdfViewerDrawer,
  })),
);

/**
 * Lists every artifact produced for the session in one place:
 *  - the **invoice** (rendered by HIE on completion, correlated by Category = sessionId) with
 *    inline preview / editable AcroForm via the PDF drawer, and
 *  - the PDMS **reports** (discharge letter, billing summary, shift roll-up) with download.
 *
 * Both download through the authenticated apiClient — the binary endpoints require a Bearer
 * token, so a plain anchor would 401. Drives the live-session "Documents" tab.
 */
type Props = { sessionId: string; patientId?: string };

export const SessionReportsTab = ({ sessionId, patientId }: Props) => {
  const [openInvoiceId, setOpenInvoiceId] = useState<string | null>(null);

  const reportsQuery = useQuery({
    queryKey: ["pdms", "sessions", sessionId, "reports"],
    queryFn: () => fetchSessionReports(sessionId),
    refetchInterval: 30_000,
  });

  // Invoices are HIE DocumentReferences (Kind=invoice) correlated to the session via Category.
  // We scope the list by patient when known to keep it small, then match the session client-side.
  const invoicesQuery = useQuery({
    queryKey: ["hie", "documents", "session-invoices", sessionId, patientId],
    queryFn: () => fetchDocuments({ patientId, kind: "invoice", take: 200 }),
    refetchInterval: 30_000,
    enabled: Boolean(sessionId),
  });
  const invoices = (invoicesQuery.data ?? []).filter((d) => d.category === sessionId);

  const reports = reportsQuery.data ?? [];
  const nothingYet =
    !reportsQuery.isLoading &&
    !invoicesQuery.isLoading &&
    reports.length === 0 &&
    invoices.length === 0;

  return (
    <div className="space-y-5">
      <section>
        <h3 className="mb-2 text-xs font-semibold uppercase tracking-wider text-slate-400">
          Invoice
        </h3>
        <InvoiceSection
          isLoading={invoicesQuery.isLoading}
          invoices={invoices}
          onPreview={setOpenInvoiceId}
        />
      </section>

      <section>
        <h3 className="mb-2 text-xs font-semibold uppercase tracking-wider text-slate-400">
          Reports
        </h3>
        <ReportsSection
          isLoading={reportsQuery.isLoading}
          isError={reportsQuery.isError}
          reports={reports}
        />
      </section>

      {nothingYet && (
        <p className="text-xs text-slate-500">
          Documents appear here once the session is completed.
        </p>
      )}

      {openInvoiceId && (
        <Suspense fallback={null}>
          <PdfViewerDrawer documentId={openInvoiceId} onClose={() => setOpenInvoiceId(null)} />
        </Suspense>
      )}
    </div>
  );
};

const InvoiceSection = ({
  isLoading,
  invoices,
  onPreview,
}: {
  isLoading: boolean;
  invoices: DocumentRow[];
  onPreview: (id: string) => void;
}) => {
  if (isLoading) return <div className="text-sm text-slate-400">Loading invoice…</div>;
  if (invoices.length === 0) {
    return (
      <div className="rounded border border-dashed border-slate-700 p-4 text-xs text-slate-500">
        The invoice is rendered a few seconds after the session completes (PDMS → EHR → HIE).
      </div>
    );
  }
  return (
    <ul className="space-y-2">
      {invoices.map((doc) => (
        <InvoiceRow key={doc.id} doc={doc} onPreview={() => onPreview(doc.id)} />
      ))}
    </ul>
  );
};

const ReportsSection = ({
  isLoading,
  isError,
  reports,
}: {
  isLoading: boolean;
  isError: boolean;
  reports: SessionReport[];
}) => {
  if (isLoading) return <div className="text-sm text-slate-400">Loading reports…</div>;
  if (isError) {
    return <div className="text-sm text-rose-300">Could not load reports. Retry shortly.</div>;
  }
  if (reports.length === 0) {
    return (
      <div className="rounded border border-dashed border-slate-700 p-4 text-xs text-slate-500">
        Reports are generated when the session completes — none yet for this session.
      </div>
    );
  }
  return (
    <ul className="space-y-2">
      {reports.map((row) => (
        <ReportRow key={row.id} report={row} />
      ))}
    </ul>
  );
};

const InvoiceRow = ({ doc, onPreview }: { doc: DocumentRow; onPreview: () => void }) => (
  <li className="flex items-center justify-between rounded border border-slate-800 bg-slate-900/40 p-3 text-sm">
    <div>
      <div className="font-medium text-slate-100">{doc.title}</div>
      <div className="text-xs text-slate-500">
        {new Date(doc.createdAtUtc).toISOString().slice(0, 19)} UTC · {Math.round(doc.size / 1024)}{" "}
        KB
        {doc.hasAcroForms && (
          <span className="ml-2 rounded bg-emerald-900/40 px-1.5 py-0.5 text-emerald-200">
            editable
          </span>
        )}
      </div>
    </div>
    <div className="flex gap-2">
      <button
        type="button"
        onClick={onPreview}
        className="rounded bg-sky-600 px-3 py-1.5 text-xs text-slate-50 hover:bg-sky-500"
      >
        Preview / edit
      </button>
      <button
        type="button"
        onClick={() => void downloadDocumentBinary(doc.id, `${doc.title}.pdf`)}
        className="rounded border border-slate-700 px-3 py-1.5 text-xs text-slate-200 hover:border-slate-500"
      >
        Download
      </button>
    </div>
  </li>
);

const ReportRow = ({ report }: { report: SessionReport }) => {
  const ready = report.status === "Generated" || report.status === "Delivered";
  return (
    <li className="flex items-center justify-between rounded border border-slate-800 bg-slate-900/40 p-3 text-sm">
      <div>
        <div className="font-medium text-slate-100">{labelFor(report.kind)}</div>
        <div className="text-xs text-slate-500">
          {report.generatedAtUtc
            ? `Generated ${new Date(report.generatedAtUtc).toISOString().slice(0, 19)} UTC`
            : "Pending"}
          {" · "}
          Status: <span className="font-mono">{report.status}</span>
          {report.contentHash ? (
            <span className="ml-2 font-mono text-slate-600" title="SHA-256 of the rendered bytes">
              {report.contentHash.slice(0, 12)}…
            </span>
          ) : null}
        </div>
        {report.failureReason ? (
          <div className="mt-1 text-xs text-rose-300">Failure: {report.failureReason}</div>
        ) : null}
      </div>
      <button
        type="button"
        disabled={!ready}
        onClick={() => void downloadReportBinary(report.id, `${labelFor(report.kind)}.pdf`)}
        className="rounded border border-slate-700 px-3 py-1.5 text-xs text-slate-200 hover:border-slate-500 disabled:cursor-not-allowed disabled:opacity-40"
      >
        Download PDF
      </button>
    </li>
  );
};

const labelFor = (kind: SessionReport["kind"]): string => {
  switch (kind) {
    case "DischargeLetter":
      return "Discharge letter";
    case "BillingDocument":
      return "Billing summary";
    case "ShiftReport":
      return "Shift report";
  }
};
