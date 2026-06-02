import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  fetchTemplates,
  appendTemplateVersion,
  publishTemplate,
  type ReportTemplate,
} from "@/features/reports/api/reportsApi";
import { useAuth } from "@/features/auth/components/AuthProvider";

/**
 * Operator-facing template authoring. Lists every `ReportTemplate` aggregate the API
 * returns; clicking a row opens the version stack with publish + rollback controls.
 * A "New template" button appends a fresh draft against the operator-supplied slug +
 * kind, which they then publish via the same flow once they've authored the body.
 */
export const ReportingTemplatesPage = () => {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const [selected, setSelected] = useState<ReportTemplate | null>(null);
  const [creating, setCreating] = useState(false);

  const query = useQuery({
    queryKey: ["pdms", "reporting", "templates"],
    queryFn: () => fetchTemplates(),
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["pdms", "reporting", "templates"] });
  const rows = query.data ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold text-slate-100">Reporting templates</h1>
          <p className="text-sm text-slate-400">
            Operator-authored Markdown + Mustache templates for discharge letters, shift reports,
            and billing summaries. Publish flips the active version.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setCreating(true)}
          className="rounded bg-emerald-600 px-3 py-1.5 text-sm text-slate-50 hover:bg-emerald-500"
        >
          New template
        </button>
      </div>

      {query.isLoading && <div className="text-sm text-slate-400">Loading templates…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">Could not load templates. Retry shortly.</div>
      )}

      {!query.isLoading && rows.length === 0 && (
        <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
          No templates authored yet. The platform falls back to a built-in body until you publish
          one.
        </div>
      )}

      <ul className="space-y-2">
        {rows.map((t) => (
          <li
            key={t.id}
            className="rounded border border-slate-800 bg-slate-900/40 p-3 text-sm hover:border-slate-700"
          >
            <button type="button" onClick={() => setSelected(t)} className="block w-full text-left">
              <div className="flex items-center justify-between">
                <span className="font-medium text-slate-100">{t.title}</span>
                <span className="font-mono text-xs text-slate-400">
                  {t.slug} · {t.kind}
                </span>
              </div>
              <div className="mt-1 text-xs text-slate-500">
                Published version:{" "}
                {t.publishedVersionNumber ?? <span className="text-amber-300">none</span>} ·{" "}
                {t.versions.length} version{t.versions.length === 1 ? "" : "s"} in history
              </div>
            </button>
          </li>
        ))}
      </ul>

      {selected && (
        <VersionDrawer
          template={selected}
          onClose={() => setSelected(null)}
          onApplied={() => {
            invalidate();
            setSelected(null);
          }}
        />
      )}

      {creating && user && (
        <NewTemplateDrawer
          actorSub={user.username}
          onClose={() => setCreating(false)}
          onApplied={() => {
            invalidate();
            setCreating(false);
          }}
        />
      )}
    </div>
  );
};

const VersionDrawer = ({
  template,
  onClose,
  onApplied,
}: {
  template: ReportTemplate;
  onClose: () => void;
  onApplied: () => void;
}) => {
  const publish = useMutation({
    mutationFn: (versionNumber: number) => publishTemplate(template.slug, versionNumber),
    onSuccess: onApplied,
  });

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-end bg-slate-950/70" role="dialog">
      <div className="h-full w-full max-w-2xl overflow-y-auto border-l border-slate-800 bg-slate-900 p-5 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <div>
            <h2 className="text-lg font-semibold text-slate-100">{template.title}</h2>
            <div className="text-xs text-slate-500">
              {template.slug} · {template.kind}
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-700 px-3 py-1.5 text-sm text-slate-200 hover:border-slate-500"
          >
            Close
          </button>
        </div>

        <ul className="space-y-3 text-sm">
          {template.versions.map((v) => {
            const isPublished = template.publishedVersionNumber === v.versionNumber;
            return (
              <li
                key={v.versionNumber}
                className="rounded border border-slate-800 bg-slate-900/40 p-3"
              >
                <div className="flex items-center justify-between">
                  <span className="font-medium text-slate-100">
                    Version {v.versionNumber}{" "}
                    {isPublished && (
                      <span className="ml-2 rounded bg-emerald-900/40 px-1.5 py-0.5 text-xs text-emerald-200">
                        Published
                      </span>
                    )}
                  </span>
                  {!isPublished && (
                    <button
                      type="button"
                      onClick={() => publish.mutate(v.versionNumber)}
                      disabled={publish.isPending}
                      className="rounded border border-slate-700 px-3 py-1 text-xs text-slate-200 hover:border-slate-500 disabled:opacity-50"
                    >
                      {publish.isPending
                        ? "Publishing…"
                        : isPublishedRecent(template, v.versionNumber)
                          ? "Roll back"
                          : "Publish"}
                    </button>
                  )}
                </div>
                <div className="mt-1 text-xs text-slate-500">
                  Authored by {v.authoredBySub} on{" "}
                  {new Date(v.authoredAtUtc).toISOString().slice(0, 19)} UTC
                </div>
                <pre className="mt-2 max-h-64 overflow-auto rounded bg-slate-950/40 p-2 text-xs text-slate-300">
                  {v.bodyMarkdown}
                </pre>
              </li>
            );
          })}
        </ul>
      </div>
    </div>
  );
};

const NewTemplateDrawer = ({
  actorSub,
  onClose,
  onApplied,
}: {
  actorSub: string;
  onClose: () => void;
  onApplied: () => void;
}) => {
  const [slug, setSlug] = useState("");
  const [title, setTitle] = useState("");
  const [kind, setKind] = useState<ReportTemplate["kind"]>("DischargeLetter");
  const [body, setBody] = useState("");

  const mutation = useMutation({
    mutationFn: () =>
      appendTemplateVersion({
        slug,
        title,
        kind,
        bodyMarkdown: body,
        authoredBySub: actorSub,
      }),
    onSuccess: onApplied,
  });

  const canSubmit = slug.trim() && title.trim() && body.trim() && !mutation.isPending;

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-end bg-slate-950/70" role="dialog">
      <div className="h-full w-full max-w-2xl overflow-y-auto border-l border-slate-800 bg-slate-900 p-5 shadow-xl">
        <h2 className="mb-4 text-lg font-semibold text-slate-100">New template</h2>
        <div className="space-y-3 text-sm">
          <label className="block">
            <span className="text-slate-400">Slug</span>
            <input
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100 font-mono"
              value={slug}
              onChange={(e) => setSlug(e.target.value)}
              placeholder="discharge-letter-de"
            />
          </label>
          <label className="block">
            <span className="text-slate-400">Title</span>
            <input
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Entlassungsbrief"
            />
          </label>
          <label className="block">
            <span className="text-slate-400">Kind</span>
            <select
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2"
              value={kind}
              onChange={(e) => setKind(e.target.value as ReportTemplate["kind"])}
            >
              <option value="DischargeLetter">DischargeLetter</option>
              <option value="ShiftReport">ShiftReport</option>
              <option value="BillingDocument">BillingDocument</option>
            </select>
          </label>
          <label className="block">
            <span className="text-slate-400">Markdown body</span>
            <textarea
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100 font-mono text-xs"
              rows={14}
              value={body}
              onChange={(e) => setBody(e.target.value)}
              placeholder="# {{patient.name}}&#10;&#10;Completed {{session.modality}} on {{session.completed}}…"
            />
          </label>
        </div>

        <div className="mt-5 flex justify-end gap-2 text-sm">
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-700 px-3 py-1.5 text-slate-200 hover:border-slate-500"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={() => mutation.mutate()}
            disabled={!canSubmit}
            className="rounded bg-emerald-600 px-3 py-1.5 text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
          >
            {mutation.isPending ? "Saving…" : "Save draft"}
          </button>
        </div>
      </div>
    </div>
  );
};

// The published version stays "Roll back" labelled when the user has clicked into an
// older version with the intention of reverting; surfacing the same button consistently
// avoids two distinct buttons that perform the same backend operation.
const isPublishedRecent = (template: ReportTemplate, versionNumber: number): boolean =>
  template.publishedVersionNumber !== null &&
  template.publishedVersionNumber !== undefined &&
  versionNumber < template.publishedVersionNumber;

export default ReportingTemplatesPage;
