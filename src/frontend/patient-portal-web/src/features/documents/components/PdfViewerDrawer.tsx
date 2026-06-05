import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Document, Page, pdfjs } from "react-pdf";
import type { PDFDocumentProxy } from "pdfjs-dist";
import workerSrc from "pdfjs-dist/build/pdf.worker.min.mjs?url";
import "react-pdf/dist/Page/AnnotationLayer.css";
import "react-pdf/dist/Page/TextLayer.css";
import {
  deleteDocument,
  documentBinaryUrl,
  downloadDocumentBinary,
  fetchDocumentDetail,
  fetchDocumentPreview,
  fillDocumentAcroForm,
  setJavaScriptExecution,
  signDocument,
  type PadesLevel,
  type SignerKind,
} from "@/features/documents/api/documentsApi";
import { useAuth } from "@/features/auth/components/AuthProvider";

// pdfjs worker is shipped as an ES module; Vite resolves it via the ?url import. With
// the worker wired the viewer can render annotations + AcroForm widgets inline.
pdfjs.GlobalWorkerOptions.workerSrc = workerSrc;

type Props = {
  documentId: string;
  onClose: () => void;
};

/** A single editable AcroForm field surfaced from the PDF via pdfjs. */
type AcroField = {
  name: string;
  type: "text" | "checkbox" | "combobox";
  required: boolean;
  multiline: boolean;
  options: string[];
};

// Friendly labels for the invoice's fields; falls back to the raw field name for any other PDF.
const FIELD_LABELS: Record<string, string> = {
  BillToName: "Bill to (name)",
  BillToAddress: "Bill to (address)",
  PayerCode: "Payer",
  PoNumber: "PO number",
  Remarks: "Remarks",
  Reviewed: "Reviewed",
};

// Fields we always treat as required even if a viewer doesn't surface the AcroForm Required flag.
const ALWAYS_REQUIRED = new Set(["BillToName", "PayerCode"]);

const labelFor = (name: string): string => FIELD_LABELS[name] ?? name;

type PdfjsWidget = {
  type?: string;
  value?: unknown;
  required?: boolean;
  multiline?: boolean;
  items?: { displayValue?: string; exportValue?: string }[];
};

/** Maps a single pdfjs widget into a typed field + its initial value, or null if unsupported. */
const mapWidget = (name: string, w: PdfjsWidget): { field: AcroField; value: string } | null => {
  const required = Boolean(w.required) || ALWAYS_REQUIRED.has(name);
  const stringValue = typeof w.value === "string" ? w.value : "";
  if (w.type === "text") {
    return {
      field: { name, type: "text", required, multiline: Boolean(w.multiline), options: [] },
      value: stringValue,
    };
  }
  if (w.type === "combobox" || w.type === "listbox") {
    const options = (w.items ?? [])
      .map((i) => i.exportValue ?? i.displayValue ?? "")
      .filter((o) => o.length > 0);
    return {
      field: { name, type: "combobox", required, multiline: false, options },
      value: stringValue,
    };
  }
  if (w.type === "checkbox") {
    const checked = w.value === true || (stringValue !== "Off" && stringValue !== "");
    return {
      field: { name, type: "checkbox", required, multiline: false, options: [] },
      value: checked ? "true" : "false",
    };
  }
  return null;
};

/** Maps pdfjs field objects into the typed, editable field list the drawer renders. */
const toAcroFields = (
  fieldObjects: Record<string, PdfjsWidget[]> | null,
): { fields: AcroField[]; values: Record<string, string> } => {
  const fields: AcroField[] = [];
  const values: Record<string, string> = {};
  if (!fieldObjects) return { fields, values };

  for (const [name, widgets] of Object.entries(fieldObjects)) {
    const mapped = mapWidget(name, widgets?.[0] ?? {});
    if (mapped) {
      fields.push(mapped.field);
      values[name] = mapped.value;
    }
  }
  return { fields, values };
};

/**
 * Side drawer that previews a single clinical document. PDFs render through pdfjs with
 * AcroForm widgets visible inline. PDF JavaScript stays inert by default — pdfjs's
 * `enableScripting` flag flips only when an admin has explicitly authorized JS execution
 * for this document (the `allowJavaScriptExecution` policy on the DocumentReference).
 *
 * Non-PDF previews go through the `/preview` envelope: XML / CDA / FHIR-XML are pretty
 * printed server-side and shown in a code panel; plain text is shown verbatim; everything
 * else (Office, images) falls back to a download-only card with a link to `/binary`.
 *
 * The drawer also exposes the signature history, the platform / per-user / TSP sign
 * action, server-side AcroForm fill, and the per-document JavaScript-execution toggle.
 */
export const PdfViewerDrawer = ({ documentId, onClose }: Props) => {
  const queryClient = useQueryClient();
  const { getAccessToken } = useAuth();
  const [numPages, setNumPages] = useState<number | null>(null);
  const [signMode, setSignMode] = useState<SignerKind>("Platform");
  const [signerUserId, setSignerUserId] = useState("");
  const [signReason, setSignReason] = useState("");
  const [signLevel, setSignLevel] = useState<PadesLevel>("LT");
  const [tspCredentialId, setTspCredentialId] = useState("");
  const [fields, setFields] = useState<AcroField[]>([]);
  const [formValues, setFormValues] = useState<Record<string, string>>({});
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  // Bumped after a successful fill so the <Document> re-fetches the (now baked) bytes.
  const [reloadKey, setReloadKey] = useState(0);
  const [fillFeedback, setFillFeedback] = useState<{ filled: string[]; unknown: string[] } | null>(
    null,
  );

  const detail = useQuery({
    queryKey: ["hie", "documents", documentId],
    queryFn: () => fetchDocumentDetail(documentId),
    enabled: !!documentId,
  });

  const isPdf = detail.data?.mimeType === "application/pdf";

  const preview = useQuery({
    queryKey: ["hie", "documents", documentId, "preview"],
    queryFn: () => fetchDocumentPreview(documentId),
    enabled: !!documentId && detail.data != null && !isPdf,
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["hie", "documents"], exact: false });

  const signMutation = useMutation({
    mutationFn: () =>
      signDocument(documentId, {
        certificateSource: signMode,
        level: signLevel,
        userId: signMode === "User" ? signerUserId.trim() || undefined : undefined,
        tspCredentialId: signMode === "RemoteQes" ? tspCredentialId.trim() || undefined : undefined,
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

  const fillMutation = useMutation({
    mutationFn: (values: Record<string, string>) => fillDocumentAcroForm(documentId, values),
    onSuccess: (result) => {
      setFillFeedback({ filled: result.filledFieldNames, unknown: result.unknownFields });
      // Re-fetch the now-baked bytes so the preview reflects the saved values.
      setReloadKey((k) => k + 1);
      invalidate();
    },
  });

  const loadFormFields = async (pdf: PDFDocumentProxy) => {
    setNumPages(pdf.numPages);
    try {
      const objects = await pdf.getFieldObjects();
      const mapped = toAcroFields(objects);
      setFields(mapped.fields);
      setFormValues(mapped.values);
      setFormErrors({});
    } catch {
      setFields([]);
    }
  };

  const updateField = (name: string, value: string) => {
    setFormValues((prev) => ({ ...prev, [name]: value }));
    setFormErrors((prev) => {
      if (!prev[name]) return prev;
      const next = { ...prev };
      delete next[name];
      return next;
    });
  };

  const saveForm = () => {
    const errors: Record<string, string> = {};
    for (const f of fields) {
      if (f.required && f.type !== "checkbox" && !formValues[f.name]?.trim()) {
        errors[f.name] = "This field is required.";
      }
    }
    setFormErrors(errors);
    if (Object.keys(errors).length === 0) fillMutation.mutate(formValues);
  };

  const fieldInputClass =
    "mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100";

  const renderFieldInput = (f: AcroField) => {
    const aria = labelFor(f.name);
    if (f.type === "checkbox") {
      return (
        <input
          type="checkbox"
          aria-label={aria}
          checked={formValues[f.name] === "true"}
          onChange={(e) => updateField(f.name, e.target.checked ? "true" : "false")}
          className="ml-2 align-middle accent-emerald-500"
        />
      );
    }
    if (f.type === "combobox") {
      return (
        <select
          aria-label={aria}
          value={formValues[f.name] ?? ""}
          onChange={(e) => updateField(f.name, e.target.value)}
          className={fieldInputClass}
        >
          <option value="">— select —</option>
          {f.options.map((o) => (
            <option key={o} value={o}>
              {o}
            </option>
          ))}
        </select>
      );
    }
    if (f.multiline) {
      return (
        <textarea
          aria-label={aria}
          value={formValues[f.name] ?? ""}
          onChange={(e) => updateField(f.name, e.target.value)}
          rows={2}
          className={fieldInputClass}
        />
      );
    }
    return (
      <input
        type="text"
        aria-label={aria}
        value={formValues[f.name] ?? ""}
        onChange={(e) => updateField(f.name, e.target.value)}
        className={fieldInputClass}
      />
    );
  };

  const jsToggleMutation = useMutation({
    mutationFn: (allow: boolean) => setJavaScriptExecution(documentId, allow),
    onSuccess: () => invalidate(),
  });

  const doc = detail.data;
  const canSign =
    (signMode === "Platform" && true) ||
    (signMode === "User" && signerUserId.trim().length > 0) ||
    (signMode === "RemoteQes" && tspCredentialId.trim().length > 0);

  // pdfjs's enableScripting opens up AcroForm calc + OpenAction + /AA events to the
  // document JS sandbox. We only enable it when the server has flipped the per-document
  // gate — which itself requires the retention-admin role. Everything stays inert by default.
  const enableScripting = isPdf && doc?.allowJavaScriptExecution === true;

  // Memoize options so react-pdf doesn't re-fetch the worker / document on every render.
  const pdfDocumentOptions = useMemo(
    () => ({
      isEvalSupported: enableScripting,
      enableXfa: enableScripting,
    }),
    [enableScripting],
  );

  // react-pdf fetches the bytes itself (it doesn't go through the axios apiClient), so the Bearer
  // token the interceptor normally attaches is missing and the module API 401s. Pass the token as
  // an explicit httpHeader on the pdfjs request Source. The `?v=` bumps after a fill so the baked
  // bytes re-fetch. Memoized on the token + reloadKey so the document isn't re-fetched per render.
  const pdfFileSource = useMemo(() => {
    const token = getAccessToken();
    return {
      url: `${documentBinaryUrl(documentId)}?v=${reloadKey}`,
      httpHeaders: token ? { Authorization: `Bearer ${token}` } : undefined,
      withCredentials: true,
    };
  }, [documentId, reloadKey, getAccessToken]);

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
                <span
                  className={
                    "ml-2 rounded px-1.5 py-0.5 " +
                    (doc.allowJavaScriptExecution
                      ? "bg-rose-900/40 text-rose-200"
                      : "bg-amber-900/40 text-amber-200")
                  }
                >
                  {doc.allowJavaScriptExecution
                    ? "JS execution ENABLED in viewer"
                    : "JS preserved — inert in viewer"}
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
            {isPdf ? (
              <Document
                key={reloadKey}
                file={pdfFileSource}
                onLoadSuccess={(pdf) => void loadFormFields(pdf)}
                options={pdfDocumentOptions}
                loading={<div className="text-sm text-slate-400">Loading PDF…</div>}
                error={<div className="text-sm text-rose-300">Could not render PDF.</div>}
              >
                {Array.from({ length: numPages ?? 0 }, (_, i) => (
                  <Page
                    key={`page_${i + 1}`}
                    pageNumber={i + 1}
                    renderForms
                    renderAnnotationLayer
                    renderTextLayer
                    width={640}
                  />
                ))}
              </Document>
            ) : (
              <NonPdfPreview
                preview={preview.data}
                isLoading={preview.isLoading}
                mimeType={doc?.mimeType ?? ""}
                documentId={documentId}
                filename={doc?.title ?? `document-${documentId}`}
              />
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
                    {s.tspId && <span> · TSP {s.tspId}</span>}
                  </div>
                  <div className="font-mono text-[10px] text-slate-500">{s.certThumbprint}</div>
                  <div>{new Date(s.signedAtUtc).toLocaleString()}</div>
                  <div className="text-[10px] text-slate-500">
                    PAdES-{s.padesLevel} · {s.signatureFormat}
                    {s.timestampedAtUtc && <span> · TSA-stamped</span>}
                    {s.revocationEvidenceFormat !== "None" && (
                      <span> · {s.revocationEvidenceFormat}</span>
                    )}
                  </div>
                  {s.reason && <div className="italic text-slate-400">{s.reason}</div>}
                </li>
              ))}
            </ul>

            {doc?.hasAcroForms && (
              <>
                <h3 className="mb-3 text-sm font-semibold text-slate-200">
                  Review &amp; edit (AcroForm)
                </h3>
                <p className="mb-2 text-[11px] text-slate-500">
                  Edit the fields below and Save — values are validated, baked into the PDF, and the
                  preview refreshes. Demo only (no claim submission).
                </p>
                {fields.length === 0 ? (
                  <p className="mb-4 text-[11px] text-slate-500">Loading form fields…</p>
                ) : (
                  <div className="mb-3 space-y-3">
                    {fields.map((f) => (
                      <label key={f.name} className="block text-xs">
                        <span className="text-slate-400">
                          {labelFor(f.name)}
                          {f.required && <span className="text-rose-400"> *</span>}
                        </span>
                        {renderFieldInput(f)}
                        {formErrors[f.name] && (
                          <span className="mt-1 block text-rose-300">{formErrors[f.name]}</span>
                        )}
                      </label>
                    ))}
                  </div>
                )}
                {fillFeedback && (
                  <div className="mb-2 text-[11px] text-slate-300">
                    <div>Saved {fillFeedback.filled.length} field(s) into the PDF.</div>
                    {fillFeedback.unknown.length > 0 && (
                      <div className="text-amber-300">
                        Ignored: {fillFeedback.unknown.join(", ")}
                      </div>
                    )}
                  </div>
                )}
                {fillMutation.isError && (
                  <div className="mb-2 text-xs text-rose-300">Save failed — retry shortly.</div>
                )}
                <button
                  type="button"
                  onClick={saveForm}
                  disabled={fillMutation.isPending || fields.length === 0}
                  className="mb-6 w-full rounded bg-sky-600 px-3 py-2 text-slate-50 hover:bg-sky-500 disabled:opacity-50"
                >
                  {fillMutation.isPending ? "Saving…" : "Save changes"}
                </button>
              </>
            )}

            {doc?.hasJavascript && (
              <>
                <h3 className="mb-3 text-sm font-semibold text-slate-200">JavaScript execution</h3>
                <p className="mb-2 text-[11px] text-slate-500">
                  The viewer runs embedded PDF JavaScript (calc actions / OpenAction / /AA) only
                  when this policy is on. Default is off. Audited.
                </p>
                <button
                  type="button"
                  onClick={() => jsToggleMutation.mutate(!doc.allowJavaScriptExecution)}
                  disabled={jsToggleMutation.isPending}
                  className={
                    "mb-6 w-full rounded px-3 py-2 text-slate-50 disabled:opacity-50 " +
                    (doc.allowJavaScriptExecution
                      ? "bg-rose-700 hover:bg-rose-600"
                      : "border border-amber-700 text-amber-200 hover:border-amber-500")
                  }
                >
                  {jsToggleMutation.isPending
                    ? "Updating…"
                    : doc.allowJavaScriptExecution
                      ? "Disable JS execution"
                      : "Authorize JS execution"}
                </button>
              </>
            )}

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
                  <option value="RemoteQes">TSP / eIDAS-QES</option>
                </select>
              </label>
              <label className="block">
                <span className="text-slate-400">PAdES level</span>
                <select
                  value={signLevel}
                  onChange={(e) => setSignLevel(e.target.value as PadesLevel)}
                  className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
                >
                  <option value="B">B — baseline (no TSA)</option>
                  <option value="T">T — TSA-stamped time</option>
                  <option value="LT">LT — TSA + DSS (recommended)</option>
                  <option value="LTA">LTA — LT + doc timestamp</option>
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
              {signMode === "RemoteQes" && (
                <label className="block">
                  <span className="text-slate-400">TSP credential id</span>
                  <input
                    type="text"
                    value={tspCredentialId}
                    onChange={(e) => setTspCredentialId(e.target.value)}
                    placeholder="csc-credential-uuid"
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

type NonPdfPreviewProps = {
  preview: Awaited<ReturnType<typeof fetchDocumentPreview>> | undefined;
  isLoading: boolean;
  mimeType: string;
  documentId: string;
  filename: string;
};

const NonPdfPreview = ({
  preview,
  isLoading,
  mimeType,
  documentId,
  filename,
}: NonPdfPreviewProps) => {
  // The /binary endpoint requires a Bearer token, so download via the authenticated apiClient
  // (object-URL save) rather than a plain anchor that would 401 against the module API.
  const download = () => void downloadDocumentBinary(documentId, filename);

  if (isLoading) return <div className="text-sm text-slate-400">Loading preview…</div>;
  if (!preview)
    return (
      <div className="text-sm text-slate-400">
        Preview unavailable.{" "}
        <button type="button" className="text-sky-300 underline" onClick={download}>
          Download the original
        </button>{" "}
        ({mimeType}).
      </div>
    );

  if (preview.format === "Xml") {
    return (
      <div className="space-y-2">
        <div className="text-xs text-slate-400">
          {preview.documentTypeName ?? "XML"}
          {preview.rootElement && <span> · &lt;{preview.rootElement}&gt;</span>}
        </div>
        <pre className="overflow-auto whitespace-pre-wrap rounded border border-slate-800 bg-slate-900 p-3 text-xs text-emerald-200">
          {preview.content}
        </pre>
      </div>
    );
  }
  if (preview.format === "Text") {
    return (
      <pre className="overflow-auto whitespace-pre-wrap rounded border border-slate-800 bg-slate-900 p-3 text-xs text-slate-200">
        {preview.content}
      </pre>
    );
  }
  // "Binary" (Office docs / images / etc.) — download-only.
  return (
    <div className="rounded border border-slate-800 bg-slate-900 p-4 text-sm text-slate-300">
      <div className="mb-2 font-semibold text-slate-100">Inline preview not supported</div>
      <div className="mb-3 text-xs text-slate-400">
        {mimeType} is not rendered in-app. Office documents, scanned images, and other binary kinds
        can be downloaded and opened in their native viewer.
      </div>
      <button
        type="button"
        onClick={download}
        className="inline-block rounded bg-sky-600 px-3 py-1.5 text-slate-50 hover:bg-sky-500"
      >
        Download original
      </button>
    </div>
  );
};

export default PdfViewerDrawer;
