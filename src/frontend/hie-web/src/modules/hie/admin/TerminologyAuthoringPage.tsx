import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  deleteTerminology,
  fetchAuthoredTerminology,
  upsertTerminology,
  type AuthoredTerminologyRow,
} from "@/features/terminology/api/terminologyApi";

const RESOURCE_TYPES = ["CodeSystem", "ValueSet", "ConceptMap"] as const;
const STATUSES = ["draft", "active", "retired"] as const;

const STATUS_CLASS: Record<string, string> = {
  active: "text-emerald-300",
  draft: "text-amber-300",
  retired: "text-slate-500",
};

/**
 * Authoring + versioning surface for the platform's governed terminology. The terminology lead
 * drafts a CodeSystem / ValueSet / ConceptMap (FHIR JSON), activates it, and the HIE host overlays
 * every active resource onto the in-memory catalog at startup so it serves via $validate-code /
 * $expand / $translate. A new version is a new (url, version) row.
 */
export const TerminologyAuthoringPage = () => {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<AuthoredTerminologyRow | "new" | null>(null);

  const query = useQuery({
    queryKey: ["hie", "terminology"],
    queryFn: fetchAuthoredTerminology,
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["hie", "terminology"], exact: false });
  const rows = query.data ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold text-slate-100">Terminology authoring</h1>
          <p className="text-sm text-slate-400">
            Governed CodeSystems / ValueSets / ConceptMaps. Active resources are overlaid onto the
            catalog at host startup and served by the terminology operations. A new version is a new row.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setEditing("new")}
          className="rounded bg-emerald-600 px-3 py-1.5 text-sm text-slate-50 hover:bg-emerald-500"
        >
          New resource
        </button>
      </div>

      {query.isLoading && <div className="text-sm text-slate-400">Loading…</div>}
      {query.isError && <div className="text-sm text-rose-300">Could not load resources. Retry shortly.</div>}
      {!query.isLoading && rows.length === 0 && (
        <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
          No authored terminology yet — only the built-in catalog is served. Add a resource here.
        </div>
      )}

      {rows.length > 0 && (
        <table className="w-full table-fixed border-collapse text-sm">
          <thead className="text-left text-slate-400">
            <tr>
              <th className="w-28 pb-2 font-medium">Type</th>
              <th className="pb-2 font-medium">Canonical URL</th>
              <th className="w-24 pb-2 font-medium">Version</th>
              <th className="w-20 pb-2 font-medium">Status</th>
              <th className="w-40 pb-2 font-medium">Updated</th>
              <th className="w-20 pb-2 font-medium" />
            </tr>
          </thead>
          <tbody className="text-slate-200">
            {rows.map((row) => (
              <tr key={row.id} className="border-t border-slate-800/60">
                <td className="py-2 align-top">{row.resourceType}</td>
                <td className="py-2 align-top font-mono text-xs break-all text-slate-300">{row.url}</td>
                <td className="py-2 align-top">{row.version}</td>
                <td className={`py-2 align-top ${STATUS_CLASS[row.status] ?? "text-slate-300"}`}>{row.status}</td>
                <td className="py-2 align-top font-mono text-xs">
                  {new Date(row.updatedAtUtc).toISOString().slice(0, 16).replace("T", " ")} · {row.updatedBy}
                </td>
                <td className="py-2 align-top text-right">
                  <button
                    type="button"
                    onClick={() => setEditing(row)}
                    className="rounded border border-slate-700 px-2 py-1 text-xs text-slate-200 hover:border-slate-500"
                  >
                    Edit
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {editing && (
        <TerminologyDrawer
          row={editing === "new" ? null : editing}
          onClose={() => setEditing(null)}
          onApplied={() => {
            invalidate();
            setEditing(null);
          }}
        />
      )}
    </div>
  );
};

const TerminologyDrawer = ({
  row,
  onClose,
  onApplied,
}: {
  row: AuthoredTerminologyRow | null;
  onClose: () => void;
  onApplied: () => void;
}) => {
  const [resourceType, setResourceType] = useState(row?.resourceType ?? "ValueSet");
  const [url, setUrl] = useState(row?.url ?? "");
  const [version, setVersion] = useState(row?.version ?? "1.0.0");
  const [name, setName] = useState(row?.name ?? "");
  const [status, setStatus] = useState(row?.status ?? "draft");
  const [fhirJson, setFhirJson] = useState("");

  const upsert = useMutation({
    mutationFn: () => upsertTerminology({ resourceType, url, version, status, name, fhirJson }),
    onSuccess: onApplied,
  });
  const remove = useMutation({
    mutationFn: () => deleteTerminology(row!.id),
    onSuccess: onApplied,
  });

  const canSave =
    url.trim().length > 0 && version.trim().length > 0 && name.trim().length > 0 &&
    fhirJson.trim().length > 0 && !upsert.isPending;

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-end bg-slate-950/70" role="dialog">
      <div className="h-full w-full max-w-lg overflow-y-auto border-l border-slate-800 bg-slate-900 p-5 shadow-xl">
        <h2 className="mb-3 text-lg font-semibold text-slate-100">
          {row ? `Revise ${row.name}` : "New terminology resource"}
        </h2>

        <div className="grid grid-cols-2 gap-3">
          <label className="block text-sm">
            <span className="text-slate-400">Resource type</span>
            <select
              value={resourceType}
              disabled={row !== null}
              onChange={(e) => setResourceType(e.target.value)}
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100 disabled:opacity-50"
            >
              {RESOURCE_TYPES.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </label>
          <label className="block text-sm">
            <span className="text-slate-400">Status</span>
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value)}
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
            >
              {STATUSES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </label>
        </div>

        <label className="mt-3 block text-sm">
          <span className="text-slate-400">Canonical URL</span>
          <input
            type="text"
            value={url}
            disabled={row !== null}
            onChange={(e) => setUrl(e.target.value)}
            placeholder="https://dialysis.local/fhir/ValueSet/my-set"
            className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 font-mono text-xs text-slate-100 disabled:opacity-50"
          />
        </label>

        <div className="mt-3 grid grid-cols-2 gap-3">
          <label className="block text-sm">
            <span className="text-slate-400">Version</span>
            <input
              type="text"
              value={version}
              disabled={row !== null}
              onChange={(e) => setVersion(e.target.value)}
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100 disabled:opacity-50"
            />
          </label>
          <label className="block text-sm">
            <span className="text-slate-400">Name</span>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
            />
          </label>
        </div>

        <label className="mt-3 block text-sm">
          <span className="text-slate-400">FHIR JSON</span>
          <textarea
            value={fhirJson}
            onChange={(e) => setFhirJson(e.target.value)}
            rows={12}
            placeholder='{"resourceType":"ValueSet","url":"…","status":"active", …}'
            className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 font-mono text-xs text-slate-100"
          />
        </label>
        <p className="mt-2 text-xs text-slate-500">
          The body must be a {resourceType} whose <code>url</code> matches the canonical URL above; it
          is validated server-side before it is stored.
        </p>

        {(upsert.isError || remove.isError) && (
          <div className="mt-3 text-xs text-rose-300">Save failed — check the FHIR JSON and retry.</div>
        )}

        <div className="mt-5 flex justify-between gap-2 text-sm">
          {row && (
            <button
              type="button"
              onClick={() => remove.mutate()}
              disabled={remove.isPending}
              className="rounded border border-rose-700 px-3 py-1.5 text-rose-200 hover:border-rose-500 disabled:opacity-50"
            >
              {remove.isPending ? "Deleting…" : "Delete"}
            </button>
          )}
          <div className="ml-auto flex gap-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded border border-slate-700 px-3 py-1.5 text-slate-200 hover:border-slate-500"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={() => upsert.mutate()}
              disabled={!canSave}
              className="rounded bg-emerald-600 px-3 py-1.5 text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
            >
              {upsert.isPending ? "Saving…" : "Save"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default TerminologyAuthoringPage;
