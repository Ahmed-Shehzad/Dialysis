import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createCodeTemplateLibrary,
  deleteCodeTemplateLibrary,
  fetchCodeTemplateLibraries,
  importCodeTemplateLibrariesMirthXml,
  updateCodeTemplateLibrary,
} from "../api/codeTemplates";
import { type CodeTemplateLibrary, CodeTemplateTypeLabel } from "../api/types";

const emptyLibrary = (): CodeTemplateLibrary => ({
  id: crypto.randomUUID(),
  name: "New library",
  description: null,
  linkedFlowIds: [],
  autoLinkNewFlows: false,
  revision: 0,
  lastModifiedUtc: new Date().toISOString(),
  templates: [],
});

const ImportMirthXmlButton = () => {
  const queryClient = useQueryClient();
  const mut = useMutation({
    mutationFn: (xml: string) => importCodeTemplateLibrariesMirthXml(xml),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["smartconnect", "code-template-libraries"] }),
  });
  return (
    <label className="cursor-pointer rounded-md border border-slate-700 px-2 py-1 text-xs text-slate-300 hover:bg-slate-800">
      {mut.isPending ? "Importing…" : "Import Mirth XML"}
      <input
        type="file"
        accept=".xml,text/xml,application/xml"
        className="hidden"
        onChange={async (e) => {
          const file = e.target.files?.[0];
          e.target.value = "";
          if (!file) return;
          const text = await file.text();
          mut.mutate(text);
        }}
      />
    </label>
  );
};

export const CodeTemplatesTab = () => {
  const queryClient = useQueryClient();
  const libraries = useQuery({
    queryKey: ["smartconnect", "code-template-libraries"],
    queryFn: fetchCodeTemplateLibraries,
  });
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const selected = libraries.data?.find((l) => l.id === selectedId) ?? null;

  const create = useMutation({
    mutationFn: () => createCodeTemplateLibrary(emptyLibrary()),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ["smartconnect", "code-template-libraries"] });
      setSelectedId(data.id);
    },
  });
  const updateName = useMutation({
    mutationFn: (lib: CodeTemplateLibrary) => updateCodeTemplateLibrary(lib),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["smartconnect", "code-template-libraries"] }),
  });
  const remove = useMutation({
    mutationFn: (id: string) => deleteCodeTemplateLibrary(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["smartconnect", "code-template-libraries"] });
      setSelectedId(null);
    },
  });

  return (
    <section className="grid grid-cols-1 gap-4 md:grid-cols-[260px_minmax(0,1fr)]">
      <aside className="space-y-2">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-medium text-slate-200">Libraries</h3>
          <div className="flex gap-1">
            <button
              type="button"
              onClick={() => create.mutate()}
              disabled={create.isPending}
              className="rounded-md bg-clinic-600 px-2 py-0.5 text-xs text-white hover:bg-clinic-700 disabled:opacity-40"
            >
              + New
            </button>
            <ImportMirthXmlButton />
          </div>
        </div>
        {libraries.isLoading && <div className="text-xs text-slate-400">Loading…</div>}
        {libraries.error && <div className="text-xs text-rose-300">Unavailable.</div>}
        <ul className="divide-y divide-slate-800 rounded-md border border-slate-800 bg-slate-900/40">
          {libraries.data?.map((lib) => (
            <li key={lib.id}>
              <button
                type="button"
                onClick={() => setSelectedId(lib.id)}
                className={
                  "block w-full px-3 py-2 text-left text-sm hover:bg-slate-900/60 " +
                  (selectedId === lib.id ? "bg-slate-900/80" : "")
                }
              >
                <div className="text-slate-100">{lib.name}</div>
                <div className="text-xs text-slate-500">
                  {lib.templates.length} template{lib.templates.length === 1 ? "" : "s"}
                  {lib.autoLinkNewFlows && " · auto-link"}
                </div>
              </button>
            </li>
          ))}
          {libraries.data?.length === 0 && (
            <li className="px-3 py-4 text-center text-xs text-slate-500">
              No libraries. Click + New, or import a Mirth XML export.
            </li>
          )}
        </ul>
      </aside>

      <div className="space-y-3">
        {!selected && (
          <div className="rounded-md border border-slate-800 bg-slate-900/40 p-4 text-xs text-slate-500">
            Select a library to view its templates.
          </div>
        )}
        {selected && (
          <>
            <div className="flex flex-wrap items-center justify-between gap-2">
              <input
                aria-label="Library name"
                value={selected.name}
                onChange={(e) => updateName.mutate({ ...selected, name: e.target.value })}
                className="flex-1 rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-sm text-slate-100"
              />
              <button
                type="button"
                onClick={() => {
                  if (confirm(`Delete library "${selected.name}"?`)) remove.mutate(selected.id);
                }}
                className="rounded-md border border-rose-700 px-2 py-0.5 text-xs text-rose-300 hover:bg-rose-900/40"
              >
                Delete library
              </button>
            </div>
            <textarea
              aria-label="Library description"
              value={selected.description ?? ""}
              onChange={(e) => updateName.mutate({ ...selected, description: e.target.value })}
              placeholder="Library description"
              className="w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
              rows={2}
            />

            <div className="text-xs text-slate-500">
              Linked to {selected.linkedFlowIds.length} flow
              {selected.linkedFlowIds.length === 1 ? "" : "s"}
              {selected.autoLinkNewFlows ? " · auto-links new flows" : ""} · revision{" "}
              {selected.revision}
            </div>

            {selected.templates.length === 0 ? (
              <div className="rounded-md border border-slate-800 bg-slate-900/40 p-4 text-xs text-slate-500">
                Empty library. Add templates by uploading a Mirth XML export — inline template
                editing isn't in the v1 UI; use the API or re-import.
              </div>
            ) : (
              <ul className="space-y-2">
                {selected.templates.map((t) => (
                  <li key={t.id} className="rounded-md border border-slate-800 bg-slate-900/40 p-3">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium text-slate-100">{t.name}</span>
                      <span className="text-xs text-slate-500">
                        {CodeTemplateTypeLabel[t.type] ?? t.type} · rev {t.revision}
                      </span>
                    </div>
                    {t.jsDoc && (
                      <pre className="mt-1 max-h-24 overflow-auto text-xs text-slate-400">
                        {t.jsDoc}
                      </pre>
                    )}
                    <pre className="mt-2 max-h-72 overflow-auto rounded border border-slate-800 bg-slate-950 p-2 font-mono text-xs text-slate-200">
                      {t.code}
                    </pre>
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </div>
    </section>
  );
};
