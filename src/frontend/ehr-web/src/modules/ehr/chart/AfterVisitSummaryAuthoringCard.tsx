import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import {
  addLine,
  type AvsLineKind,
  createSummary,
  DEMO_PROVIDER_ID,
  publishSummary,
} from "@/features/after-visit-summary/api/afterVisitSummaryApi";
import { notify } from "@/features/durable-commands";
import { humanizeError } from "@/lib/api/humanizeError";

type Line = { kind: AvsLineKind; text: string; url?: string };

/**
 * Clinician authoring for the patient's after-visit summary. Build a plain-language narrative plus
 * self-care instructions, follow-up actions, and resource links, then publish — which pushes the
 * summary (and a real-time toast) to the patient portal. Builds locally, then create → add lines →
 * publish in one action.
 */
export const AfterVisitSummaryAuthoringCard = ({ patientId }: { patientId: string }) => {
  const [open, setOpen] = useState(false);
  const [narrative, setNarrative] = useState("");
  const [lines, setLines] = useState<Line[]>([]);
  const [draft, setDraft] = useState<{ kind: AvsLineKind; text: string; url: string }>({
    kind: 1,
    text: "",
    url: "",
  });

  const addPendingLine = () => {
    if (draft.text.trim().length === 0) return;
    setLines((ls) => [
      ...ls,
      { kind: draft.kind, text: draft.text.trim(), url: draft.url.trim() || undefined },
    ]);
    setDraft({ kind: draft.kind, text: "", url: "" });
  };

  const publish = useMutation({
    mutationFn: async () => {
      const summaryId = await createSummary({
        patientId,
        encounterRef: crypto.randomUUID(),
        visitDateUtc: new Date().toISOString(),
        authoringProviderId: DEMO_PROVIDER_ID,
        narrative: narrative.trim(),
      });
      for (const line of lines) {
        await addLine(summaryId, line);
      }
      await publishSummary(summaryId);
    },
    onSuccess: () => {
      notify({ kind: "success", message: "After-visit summary published to the patient." });
      setNarrative("");
      setLines([]);
      setOpen(false);
    },
  });

  const kindLabel = (k: AvsLineKind): string =>
    k === 1 ? "Instruction" : k === 2 ? "Follow-up" : "Resource";

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header className="flex items-center justify-between">
        <div>
          <h3 className="text-sm font-medium text-slate-200">After-visit summary</h3>
          <p className="text-xs text-slate-400">
            Plain-language recap + self-care, sent to the patient portal.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          className="rounded-md border border-slate-700 px-2.5 py-1 text-xs text-slate-200 transition hover:border-slate-500"
        >
          {open ? "Cancel" : "+ Author"}
        </button>
      </header>

      {open && (
        <div className="mt-3 space-y-3">
          <textarea
            value={narrative}
            onChange={(e) => setNarrative(e.target.value)}
            rows={3}
            placeholder="What happened at this visit, in plain language…"
            aria-label="Visit narrative"
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 text-sm text-slate-100"
          />

          {lines.length > 0 && (
            <ul className="space-y-1 text-sm">
              {lines.map((l, i) => (
                <li
                  key={i}
                  className="flex items-center justify-between gap-2 rounded-md bg-slate-800/50 px-2 py-1"
                >
                  <span className="text-slate-200">
                    <span className="mr-2 text-xs uppercase tracking-wide text-slate-500">
                      {kindLabel(l.kind)}
                    </span>
                    {l.text}
                    {l.url && <span className="ml-1 text-xs text-clinic-300">({l.url})</span>}
                  </span>
                  <button
                    type="button"
                    onClick={() => setLines((ls) => ls.filter((_, j) => j !== i))}
                    className="text-xs text-slate-400 hover:text-rose-300"
                  >
                    remove
                  </button>
                </li>
              ))}
            </ul>
          )}

          <div className="flex flex-wrap items-end gap-2">
            <select
              value={draft.kind}
              onChange={(e) =>
                setDraft((d) => ({ ...d, kind: Number(e.target.value) as AvsLineKind }))
              }
              aria-label="Line kind"
              className="rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 text-sm text-slate-100"
            >
              <option value={1}>Instruction</option>
              <option value={2}>Follow-up</option>
              <option value={3}>Resource link</option>
            </select>
            <input
              type="text"
              value={draft.text}
              onChange={(e) => setDraft((d) => ({ ...d, text: e.target.value }))}
              placeholder={draft.kind === 3 ? "Link label" : "Text"}
              aria-label={draft.kind === 3 ? "Link label" : "Line text"}
              className="flex-1 rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 text-sm text-slate-100"
            />
            {draft.kind === 3 && (
              <input
                type="url"
                value={draft.url}
                onChange={(e) => setDraft((d) => ({ ...d, url: e.target.value }))}
                placeholder="https://…"
                aria-label="Resource link URL"
                className="flex-1 rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 text-sm text-slate-100"
              />
            )}
            <button
              type="button"
              onClick={addPendingLine}
              className="rounded-md border border-slate-700 px-3 py-1.5 text-sm text-slate-200 transition hover:border-slate-500"
            >
              Add line
            </button>
          </div>

          {publish.error && <p className="text-xs text-rose-300">{humanizeError(publish.error)}</p>}

          <button
            type="button"
            onClick={() => publish.mutate()}
            disabled={publish.isPending || narrative.trim().length === 0}
            className="rounded-md bg-clinic-600 px-4 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {publish.isPending ? "Publishing…" : "Publish to patient"}
          </button>
        </div>
      )}
    </section>
  );
};
