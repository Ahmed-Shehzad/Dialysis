import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  arrayBufferToBase64,
  fetchDocuments,
  uploadDocument,
  type DocumentRow,
  type DocumentSource,
  type DocumentStatus,
} from "@/features/documents/api/documentsApi";
import { PdfViewerDrawer } from "@/features/documents/components/PdfViewerDrawer";
import { usePatientContext } from "@/shell/PatientContextProvider";

/**
 * Admin documents board. Indexes every clinical document the platform knows about —
 * PDMS-produced reports, HIE-received partner documents, admin uploads — into one
 * filterable list. Clicking a row opens the PDF viewer drawer for inline preview,
 * AcroForm interaction, and the sign / delete actions.
 */
export const DocumentsPage = () => {
  const queryClient = useQueryClient();
  const { patient } = usePatientContext();
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const [status, setStatus] = useState<DocumentStatus | "all">("Current");
  const [source, setSource] = useState<DocumentSource | "all">("all");
  const [kind, setKind] = useState("");
  const [openId, setOpenId] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["hie", "documents", { patientId: patient?.id, status, source, kind }],
    queryFn: () =>
      fetchDocuments({
        patientId: patient?.id,
        status: status === "all" ? undefined : status,
        source: source === "all" ? undefined : source,
        kind: kind.trim() || undefined,
        take: 200,
      }),
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["hie", "documents"], exact: false });

  const uploadMutation = useMutation({
    mutationFn: async (file: File) => {
      if (!patient) throw new Error("Select a patient before uploading.");
      const buffer = await file.arrayBuffer();
      const base64 = arrayBufferToBase64(buffer);
      return uploadDocument({
        patientId: patient.id,
        kind: "AdminUpload",
        title: file.name,
        mimeType: file.type || "application/octet-stream",
        base64Content: base64,
      });
    },
    onSuccess: () => invalidate(),
  });

  const rows = query.data ?? [];

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-lg font-semibold text-slate-100">Documents</h1>
          <p className="text-sm text-slate-400">
            Discharge letters, partner-received documents, and admin uploads. Click a row to
            preview, fill, sign, or delete.
          </p>
        </div>
        <div className="flex flex-wrap gap-2 text-sm">
          <select
            aria-label="Filter by status"
            value={status}
            onChange={(e) => setStatus(e.target.value as DocumentStatus | "all")}
            className="rounded border border-slate-700 bg-slate-800/60 px-2 py-1 text-slate-100"
          >
            <option value="Current">Current</option>
            <option value="Superseded">Superseded</option>
            <option value="EnteredInError">Entered in error</option>
            <option value="all">All</option>
          </select>
          <select
            aria-label="Filter by source"
            value={source}
            onChange={(e) => setSource(e.target.value as DocumentSource | "all")}
            className="rounded border border-slate-700 bg-slate-800/60 px-2 py-1 text-slate-100"
          >
            <option value="all">All sources</option>
            <option value="PdmsReporting">PDMS Reporting</option>
            <option value="HieInbound">HIE Inbound</option>
            <option value="AdminUpload">Admin upload</option>
            <option value="Billing">Billing</option>
          </select>
          <input
            type="text"
            aria-label="Filter by kind"
            value={kind}
            onChange={(e) => setKind(e.target.value)}
            placeholder="Filter by kind…"
            className="rounded border border-slate-700 bg-slate-800/60 px-2 py-1 text-slate-100"
          />
          <input
            ref={fileInputRef}
            type="file"
            aria-label="Upload document"
            className="hidden"
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (!file) return;
              uploadMutation.mutate(file);
              if (fileInputRef.current) fileInputRef.current.value = "";
            }}
          />
          <button
            type="button"
            onClick={() => fileInputRef.current?.click()}
            disabled={!patient || uploadMutation.isPending}
            className="rounded bg-emerald-600 px-3 py-1.5 text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
          >
            {uploadMutation.isPending ? "Uploading…" : "Upload PDF"}
          </button>
        </div>
      </div>

      {!patient && (
        <div className="rounded border border-amber-700/60 bg-amber-950/40 p-3 text-sm text-amber-200">
          Pick a patient from the top bar to scope the documents list and enable upload.
        </div>
      )}

      {query.isLoading && <div className="text-sm text-slate-400">Loading documents…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">Could not load documents. Retry shortly.</div>
      )}
      {!query.isLoading && rows.length === 0 && (
        <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
          No documents match the current filters.
        </div>
      )}

      {rows.length > 0 && (
        <table className="w-full table-fixed border-collapse text-sm">
          <thead className="text-left text-slate-400">
            <tr>
              <th className="pb-2 font-medium">Title</th>
              <th className="w-32 pb-2 font-medium">Kind</th>
              <th className="w-32 pb-2 font-medium">Source</th>
              <th className="w-28 pb-2 font-medium">Status</th>
              <th className="w-24 pb-2 font-medium">Signed</th>
              <th className="w-36 pb-2 font-medium">Created</th>
              <th className="w-20 pb-2 font-medium"></th>
            </tr>
          </thead>
          <tbody className="text-slate-200">
            {rows.map((row) => (
              <DocumentRowView key={row.id} row={row} onOpen={() => setOpenId(row.id)} />
            ))}
          </tbody>
        </table>
      )}

      {openId && <PdfViewerDrawer documentId={openId} onClose={() => setOpenId(null)} />}
    </div>
  );
};

const DocumentRowView = ({ row, onOpen }: { row: DocumentRow; onOpen: () => void }) => {
  return (
    <tr className="border-t border-slate-800/60">
      <td className="py-2 align-top">
        <div className="truncate font-medium text-slate-100">{row.title}</div>
        <div className="text-xs text-slate-500">{row.mimeType}</div>
      </td>
      <td className="py-2 align-top font-mono text-xs">{row.kind}</td>
      <td className="py-2 align-top text-xs">{row.source}</td>
      <td className="py-2 align-top text-xs">{row.status}</td>
      <td className="py-2 align-top text-xs">{row.signatureCount}×</td>
      <td className="py-2 align-top font-mono text-xs">
        {new Date(row.createdAtUtc).toISOString().slice(0, 16).replace("T", " ")}
      </td>
      <td className="py-2 align-top text-right">
        <button
          type="button"
          onClick={onOpen}
          className="rounded border border-slate-700 px-2 py-1 text-xs text-slate-200 hover:border-slate-500"
        >
          Open
        </button>
      </td>
    </tr>
  );
};

export default DocumentsPage;
