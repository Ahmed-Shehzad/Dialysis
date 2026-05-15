import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  abortSession,
  completeSession,
  startSession,
  type DialysisSessionSummary,
} from "../api/sessionsApi";

export type SessionLifecycleControlsProps = {
  session: DialysisSessionSummary | undefined;
};

const buttonClass =
  "rounded-md px-3 py-1.5 text-sm font-medium transition disabled:opacity-40 disabled:cursor-not-allowed";

export const SessionLifecycleControls = ({ session }: SessionLifecycleControlsProps) => {
  const queryClient = useQueryClient();
  const [achieved, setAchieved] = useState(2.5);
  const [reason, setReason] = useState("MEDICAL");

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["pdms", "sessions", "active"] });
    queryClient.invalidateQueries({ queryKey: ["sessions", session?.id, "readings"] });
  };

  const startMutation = useMutation({
    mutationFn: () => startSession(session!.id),
    onSuccess: invalidate,
  });
  const completeMutation = useMutation({
    mutationFn: () => completeSession(session!.id, achieved),
    onSuccess: invalidate,
  });
  const abortMutation = useMutation({
    mutationFn: () => abortSession(session!.id, reason),
    onSuccess: invalidate,
  });

  if (!session) return null;

  const canStart = session.status === "Scheduled";
  const canFinish = session.status === "InProgress";

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <h3 className="mb-3 text-sm font-medium text-slate-200">Session lifecycle</h3>
      <div className="flex flex-wrap items-center gap-4">
        <button
          type="button"
          onClick={() => startMutation.mutate()}
          disabled={!canStart || startMutation.isPending}
          className={`${buttonClass} bg-clinic-600 text-white hover:bg-clinic-700`}
        >
          {startMutation.isPending ? "Starting…" : "Start"}
        </button>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => completeMutation.mutate()}
            disabled={!canFinish || completeMutation.isPending}
            className={`${buttonClass} bg-emerald-600 text-white hover:bg-emerald-700`}
          >
            {completeMutation.isPending ? "Completing…" : "Complete"}
          </button>
          <label className="text-xs text-slate-400">
            UF L
            <input
              type="number"
              step="0.1"
              min={0}
              value={achieved}
              onChange={(e) => setAchieved(Number(e.target.value))}
              className="ml-2 w-20 rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-sm text-slate-100"
              disabled={!canFinish}
            />
          </label>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => abortMutation.mutate()}
            disabled={!canFinish || abortMutation.isPending}
            className={`${buttonClass} bg-rose-600 text-white hover:bg-rose-700`}
          >
            {abortMutation.isPending ? "Aborting…" : "Abort"}
          </button>
          <select
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-sm text-slate-100"
            disabled={!canFinish}
          >
            <option value="MEDICAL">Medical</option>
            <option value="MACHINE">Machine</option>
            <option value="PATIENT_REQUEST">Patient request</option>
            <option value="OTHER">Other</option>
          </select>
        </div>

        <div className="ml-auto text-xs text-slate-400">
          Current: <span className="font-mono">{session.status}</span>
        </div>
      </div>
      {(startMutation.error || completeMutation.error || abortMutation.error) && (
        <div className="mt-2 text-xs text-rose-300">
          Action failed — the server rejected the state transition.
        </div>
      )}
    </section>
  );
};
