import { useQuery } from "@tanstack/react-query";
import {
  type AfterVisitSummary,
  fetchMyAfterVisitSummaries,
} from "@/features/after-visit-summary/api/afterVisitSummaryApi";
import { humanizeError } from "@/lib/api/humanizeError";

/**
 * The patient's after-visit summaries — a plain-language recap of each visit with self-care
 * instructions, follow-up actions, and education links. Read-only; the clinician authors and publishes
 * from the chart.
 */
export const AfterVisitSummaryPanel = ({ patientId }: { patientId: string }) => {
  const summaries = useQuery({
    queryKey: ["patient-portal", "after-visit-summaries", patientId],
    queryFn: () => fetchMyAfterVisitSummaries(patientId),
    staleTime: 60_000,
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header>
        <h3 className="text-sm font-medium text-slate-200">After-visit summaries</h3>
        <p className="text-xs text-slate-400">What happened at your visits, and what to do next.</p>
      </header>

      {summaries.isLoading && <p className="text-xs text-slate-400">Loading your summaries…</p>}
      {summaries.error && <p className="text-xs text-rose-300">{humanizeError(summaries.error)}</p>}
      {summaries.data && summaries.data.length === 0 && (
        <p className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
          No visit summaries yet.
        </p>
      )}

      {summaries.data && summaries.data.length > 0 && (
        <ul className="space-y-3">
          {summaries.data.map((s: AfterVisitSummary) => (
            <li key={s.id} className="rounded-md border border-slate-700 p-3">
              <p className="text-xs text-slate-500">
                Visit {new Date(s.visitDateUtc).toLocaleDateString()}
              </p>
              <p className="mt-1 whitespace-pre-wrap text-sm text-slate-100">{s.narrative}</p>

              {s.instructions.length > 0 && (
                <Block title="Self-care">
                  {s.instructions.map((t, i) => (
                    <li key={i}>{t}</li>
                  ))}
                </Block>
              )}
              {s.followUps.length > 0 && (
                <Block title="Follow-up">
                  {s.followUps.map((t, i) => (
                    <li key={i}>{t}</li>
                  ))}
                </Block>
              )}
              {s.resourceLinks.length > 0 && (
                <div className="mt-2">
                  <p className="text-[10px] uppercase tracking-wide text-slate-500">Learn more</p>
                  <ul className="mt-1 space-y-0.5 text-sm">
                    {s.resourceLinks.map((l, i) => (
                      <li key={i}>
                        <a
                          href={l.url}
                          target="_blank"
                          rel="noreferrer"
                          className="text-clinic-300 hover:underline"
                        >
                          {l.label}
                        </a>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
};

const Block = ({ title, children }: { title: string; children: React.ReactNode }) => (
  <div className="mt-2">
    <p className="text-[10px] uppercase tracking-wide text-slate-500">{title}</p>
    <ul className="mt-1 list-disc space-y-0.5 pl-5 text-sm text-slate-200">{children}</ul>
  </div>
);
