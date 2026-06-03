import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Document, Page, pdfjs } from "react-pdf";
import workerSrc from "pdfjs-dist/build/pdf.worker.min.mjs?url";
import "react-pdf/dist/Page/AnnotationLayer.css";
import "react-pdf/dist/Page/TextLayer.css";
import {
  deleteDocument,
  documentBinaryUrl,
  fetchDocumentDetail,
  signDocument,
  type SignerKind,
} from "@/features/documents/api/documentsApi";

// pdfjs worker is shipped as an ES module; Vite resolves it via the ?url import. With
// the worker wired the viewer can render annotations + AcroForm widgets inline.
pdfjs.GlobalWorkerOptions.workerSrc = workerSrc;

type Props = {
  documentId: string;
  onClose: () => void;
};

/**
 * Side drawer that previews a single clinical document with full AcroForm interactivity
 * via pdfjs. PDF JavaScript (calc actions, OpenAction) is preserved in the bytes the
 * server returns; pdfjs's own sandbox decides whether to run it client-side. The drawer
 * also exposes signature history and the platform / per-user sign action.
 */
export const PdfViewerDrawer = ({ documentId, onClose }: Props) => {
  const queryClient = useQueryClient();
  const [numPages, setNumPages] = useState<number | null>(null);
  const [signMode, setSignMode] = useState<SignerKind>("Platform");
  const [signerUserId, setSignerUserId] = useState("");
  const [signReason, setSignReason] = useState("");

  const detail = useQuery({
    queryKey: ["hie", "documents", documentId],
    queryFn: () => fetchDocumentDetail(documentId),
    enabled: !!documentId,
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["hie", "documents"], exact: false });

  const signMutation = useMutation({
    mutationFn: () =>
      signDocument(documentId, {
        certificateSource: signMode,
        userId: signMode === "User" ? signerUserId.trim() || undefined : undefined,
        reason: signReason.trim() || undefined,
      }),
    onSuccess: () => invalidate(),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteDocument(documentId),
    onSuccess: () => {
      invalidate();
      onClose();
    },
  });

  const doc = detail.data;
  const canSign = signMode === "Platform" || signerUserId.trim().length > 0;

  return (
    <div className="fixed inset-0 z-40 flex bg-slate-950/70" role="dialog">
      <div className="ml-auto flex h-full w-full max-w-3xl flex-col border-l border-slate-800 bg-slate-900 shadow-xl">
        <header className="flex items-start justify-between border-b border-slate-800 p-4">
          <div>
            <h2 className="text-lg font-semibold text-slate-100">{doc?.title ?? "Document"}</h2>
            <p className="text-xs text-slate-400">
              {doc?.kind} · {doc?.mimeType} ·{" "}
              {doc?.size != null ? `${Math.round(doc.size / 1024)} KB` : "—"}
              {doc?.hasJavascript && (
                <span className="ml-2 rounded bg-amber-900/40 px-1.5 py-0.5 text-amber-200">
                  JS preserved — runs in viewer sandbox only
                </span>
              )}
              {doc?.hasAcroForms && (
                <span className="ml-2 rounded bg-emerald-900/40 px-1.5 py-0.5 text-emerald-200">
                  AcroForms
                </span>
              )}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-700 px-3 py-1.5 text-sm text-slate-200 hover:border-slate-500"
          >
            Close
          </button>
        </header>

        <div className="grid flex-1 grid-cols-3 gap-0 overflow-hidden">
          <div className="col-span-2 overflow-y-auto bg-slate-950 p-4">
            {doc?.mimeType === "application/pdf" ? (
              <Document
                file={documentBinaryUrl(documentId)}
                onLoadSuccess={({ numPages: n }) => setNumPages(n)}
                loading={<div className="text-sm text-slate-400">Loading PDF…</div>}
                error={<div className="text-sm text-rose-300">Could not render PDF.</div>}
              >
                {Array.from({ length: numPages ?? 0 }, (_, i) => (
                  <Page
                    key={`page_${i + 1}`}
                    pageNumber={i + 1}
                    renderAnnotationLayer
                    renderTextLayer
                    width={640}
                  />
                ))}
              </Document>
            ) : (
              <div className="text-sm text-slate-400">
                Inline preview is only available for PDFs. Download the original to view.
              </div>
            )}
          </div>

          <aside className="col-span-1 overflow-y-auto border-l border-slate-800 p-4">
            <h3 className="mb-3 text-sm font-semibold text-slate-200">Signatures</h3>
            {!doc?.signatures.length && (
              <p className="text-xs text-slate-500">No signatures yet.</p>
            )}
            <ul className="mb-6 space-y-2 text-xs text-slate-300">
              {doc?.signatures.map((s) => (
                <li key={s.id} className="rounded border border-slate-800 p-2">
                  <div>
                    <span className="font-semibold text-slate-200">{s.signerKind}</span>
                    {s.signerUserId && <span> · {s.signerUserId}</span>}
                  </div>
                  <div className="font-mono text-[10px] text-slate-500">{s.certThumbprint}</div>
                  <div>{new Date(s.signedAtUtc).toLocaleString()}</div>
                  {s.reason && <div className="italic text-slate-400">{s.reason}</div>}
                </li>
              ))}
            </ul>

            <h3 className="mb-3 text-sm font-semibold text-slate-200">Apply signature</h3>
            <div className="space-y-3 text-sm">
              <label className="block">
                <span className="text-slate-400">Cert source</span>
                <select
                  value={signMode}
                  onChange={(e) => setSignMode(e.target.value as SignerKind)}
                  className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
                >
                  <option value="Platform">Platform / SMC-B cert</option>
                  <option value="User">Per-user cert (Identity)</option>
                </select>
              </label>
              {signMode === "User" && (
                <label className="block">
                  <span className="text-slate-400">User id</span>
                  <input
                    type="text"
                    value={signerUserId}
                    onChange={(e) => setSignerUserId(e.target.value)}
                    placeholder="user-sub-from-keycloak"
                    className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
                  />
                </label>
              )}
              <label className="block">
                <span className="text-slate-400">Reason (optional)</span>
                <input
                  type="text"
                  value={signReason}
                  onChange={(e) => setSignReason(e.target.value)}
                  className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
                />
              </label>
              {signMutation.isError && (
                <div className="text-xs text-rose-300">Sign failed — retry shortly.</div>
              )}
              <button
                type="button"
                onClick={() => signMutation.mutate()}
                disabled={!canSign || signMutation.isPending}
                className="w-full rounded bg-emerald-600 px-3 py-2 text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
              >
                {signMutation.isPending ? "Signing…" : "Sign document"}
              </button>
              <button
                type="button"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
                className="w-full rounded border border-rose-700 px-3 py-2 text-rose-200 hover:border-rose-500 disabled:opacity-50"
              >
                {deleteMutation.isPending ? "Deleting…" : "Mark entered-in-error"}
              </button>
            </div>
          </aside>
        </div>
      </div>
    </div>
  );
};

export default PdfViewerDrawer;
