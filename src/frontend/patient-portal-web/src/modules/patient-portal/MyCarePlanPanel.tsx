import { useQuery } from "@tanstack/react-query";
import { fetchActiveCarePlan } from "@/features/ehr/api/ehrApi";
import { humanizeError } from "@/lib/api/humanizeError";

const goalTone = (status: string): string => {
  switch (status) {
    case "Achieved":
      return "text-emerald-300";
    case "NotAchieved":
      return "text-rose-300";
    case "InProgress":
      return "text-amber-300";
    default:
      return "text-slate-400";
  }
};

const goalLabel = (status: string): string => (status === "NotAchieved" ? "Not achieved" : status);

/**
 * Read-only view of the patient's active care plan and its goals. Patient empowerment: the patient
 * sees the plan their care team is working toward so they can participate. Authoring lives in the
 * clinician chart, not here.
 */
export const MyCarePlanPanel = ({ patientId }: { patientId: string }) => {
  const plan = useQuery({
    queryKey: ["patient-portal", "care-plan", patientId],
    queryFn: () => fetchActiveCarePlan(patientId),
    staleTime: 60_000,
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header>
        <h3 className="text-sm font-medium text-slate-200">My care plan</h3>
        <p className="text-xs text-slate-400">
          The plan your care team is working toward. Talk to clinic staff to update it.
        </p>
      </header>

      {plan.isLoading && <div className="text-xs text-slate-400">Loading your care plan…</div>}

      {plan.error && (
        <div
          role="alert"
          className="rounded-md border border-rose-700 bg-rose-900/40 p-2 text-xs text-rose-100"
        >
          {humanizeError(plan.error)}
        </div>
      )}

      {plan.data === null && !plan.isLoading && (
        <div className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
          No active care plan on file.
        </div>
      )}

      {plan.data && (
        <div className="space-y-2">
          <p className="text-sm text-slate-100">{plan.data.title}</p>
          {plan.data.goals.length === 0 ? (
            <p className="text-xs text-slate-500">No goals recorded yet.</p>
          ) : (
            <ul className="divide-y divide-slate-800 text-sm">
              {plan.data.goals.map((g) => (
                <li key={g.id} className="flex items-center justify-between gap-2 py-2">
                  <span className="text-slate-200">
                    {g.description}
                    {g.targetMeasure && (
                      <span className="ml-1 text-xs text-slate-500">({g.targetMeasure})</span>
                    )}
                  </span>
                  <span className={`text-xs ${goalTone(g.status)}`}>{goalLabel(g.status)}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </section>
  );
};
