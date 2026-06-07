import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  addCarePlanGoal,
  type CarePlanGoalStatus,
  closeCarePlan,
  createCarePlan,
  DEMO_PROVIDER_ID,
  fetchActiveCarePlan,
  updateCarePlanGoalStatus,
} from "@/features/ehr/api/ehrApi";
import { humanizeError } from "@/lib/api/humanizeError";

const GOAL_STATUSES: CarePlanGoalStatus[] = ["Proposed", "InProgress", "Achieved", "NotAchieved"];

const goalStatusTone = (status: CarePlanGoalStatus): string => {
  switch (status) {
    case "Achieved":
      return "text-emerald-300";
    case "NotAchieved":
      return "text-rose-300";
    case "InProgress":
      return "text-amber-300";
    case "Proposed":
      return "text-slate-400";
  }
};

/**
 * Structured care plan + goals for the chart. Clinicians create a plan, add trackable goals, advance
 * goal status, and complete / revoke the plan. Mirrors the read-only portal card patients see.
 */
export const CarePlanCard = ({ patientId }: { patientId: string }) => {
  const queryClient = useQueryClient();
  const [title, setTitle] = useState("");
  const [goalText, setGoalText] = useState("");

  const plan = useQuery({
    queryKey: ["ehr", "careplan", patientId],
    queryFn: () => fetchActiveCarePlan(patientId),
    enabled: Boolean(patientId),
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["ehr", "careplan", patientId] });

  const create = useMutation({
    mutationFn: () =>
      createCarePlan({ patientId, title: title.trim(), authoredByProviderId: DEMO_PROVIDER_ID }),
    onSuccess: () => {
      setTitle("");
      void invalidate();
    },
  });

  const planId = plan.data?.id;

  const addGoal = useMutation({
    mutationFn: () => addCarePlanGoal(planId as string, { description: goalText.trim() }),
    onSuccess: () => {
      setGoalText("");
      void invalidate();
    },
  });

  const setGoalStatus = useMutation({
    mutationFn: (vars: { goalId: string; status: CarePlanGoalStatus }) =>
      updateCarePlanGoalStatus(planId as string, vars.goalId, vars.status),
    onSuccess: () => void invalidate(),
  });

  const close = useMutation({
    mutationFn: (status: "Completed" | "Revoked") => closeCarePlan(planId as string, status),
    onSuccess: () => void invalidate(),
  });

  const anyError =
    create.error ?? addGoal.error ?? setGoalStatus.error ?? close.error ?? plan.error;

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <h3 className="mb-2 text-sm font-medium text-slate-200">Care plan</h3>

      {plan.isLoading && <p className="text-xs text-slate-400">Loading…</p>}
      {anyError && <p className="text-xs text-rose-300">{humanizeError(anyError)}</p>}

      {!plan.isLoading && !plan.data && (
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (title.trim()) create.mutate();
          }}
          className="flex flex-wrap items-center gap-2"
        >
          <p className="text-xs text-slate-500">No active care plan.</p>
          <input
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="New care-plan title…"
            aria-label="New care-plan title"
            className="flex-1 rounded-md border border-slate-700 bg-slate-950 p-2 text-sm text-slate-100"
          />
          <button
            type="submit"
            disabled={create.isPending || title.trim().length === 0}
            className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:opacity-50"
          >
            Create
          </button>
        </form>
      )}

      {plan.data && (
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <p className="text-sm text-slate-100">{plan.data.title}</p>
            <span className="text-xs uppercase tracking-wide text-slate-400">
              {plan.data.status}
            </span>
          </div>

          {plan.data.goals.length === 0 ? (
            <p className="text-xs text-slate-500">No goals yet.</p>
          ) : (
            <ul className="divide-y divide-slate-800 text-sm">
              {plan.data.goals.map((g) => (
                <li key={g.id} className="grid grid-cols-12 items-center gap-2 py-2">
                  <span className="col-span-7 text-slate-200">
                    {g.description}
                    {g.targetMeasure && (
                      <span className="ml-1 text-xs text-slate-500">({g.targetMeasure})</span>
                    )}
                  </span>
                  <span className={`col-span-2 text-xs ${goalStatusTone(g.status)}`}>
                    {g.status}
                  </span>
                  <select
                    value={g.status}
                    disabled={setGoalStatus.isPending}
                    aria-label="Goal status"
                    onChange={(e) =>
                      setGoalStatus.mutate({
                        goalId: g.id,
                        status: e.target.value as CarePlanGoalStatus,
                      })
                    }
                    className="col-span-3 rounded-md border border-slate-700 bg-slate-950 p-1 text-xs text-slate-100"
                  >
                    {GOAL_STATUSES.map((s) => (
                      <option key={s} value={s}>
                        {s}
                      </option>
                    ))}
                  </select>
                </li>
              ))}
            </ul>
          )}

          <form
            onSubmit={(e) => {
              e.preventDefault();
              if (goalText.trim()) addGoal.mutate();
            }}
            className="flex items-center gap-2"
          >
            <input
              value={goalText}
              onChange={(e) => setGoalText(e.target.value)}
              placeholder="Add a goal…"
              aria-label="Add a goal"
              className="flex-1 rounded-md border border-slate-700 bg-slate-950 p-2 text-sm text-slate-100"
            />
            <button
              type="submit"
              disabled={addGoal.isPending || goalText.trim().length === 0}
              className="rounded-md border border-slate-700 px-3 py-1.5 text-sm text-slate-200 transition hover:border-slate-500 disabled:opacity-50"
            >
              Add goal
            </button>
          </form>

          <div className="flex items-center justify-end gap-2">
            <button
              type="button"
              onClick={() => close.mutate("Completed")}
              disabled={close.isPending}
              className="rounded-md border border-slate-700 px-3 py-1.5 text-xs text-emerald-300 transition hover:border-slate-500 disabled:opacity-50"
            >
              Complete plan
            </button>
            <button
              type="button"
              onClick={() => close.mutate("Revoked")}
              disabled={close.isPending}
              className="rounded-md border border-slate-700 px-3 py-1.5 text-xs text-rose-300 transition hover:border-slate-500 disabled:opacity-50"
            >
              Revoke
            </button>
          </div>
        </div>
      )}
    </section>
  );
};
