import { useEffect, useId, useState } from "react";
import { createPortal } from "react-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { DEMO_PROVIDER_ID, SafetyBlockedError, startEncounter } from "@/features/ehr/api/ehrApi";
import {
  applyOrderSet,
  type ApplyOrderSetResult,
  fetchOrderSets,
  type OrderSetSummary,
} from "@/features/order-sets/api/orderSetApi";
import { humanizeError } from "@/lib/api/humanizeError";
import { SafetyAdvisoryList } from "@/modules/ehr/chart/SafetyAdvisoryList";

interface Props {
  patientId: string;
  onClose(): void;
}

const lineSummary = (s: OrderSetSummary): string =>
  [
    s.labLines > 0 ? `${s.labLines} lab` : null,
    s.medicationLines > 0 ? `${s.medicationLines} med` : null,
    s.imagingLines > 0 ? `${s.imagingLines} imaging` : null,
  ]
    .filter(Boolean)
    .join(" · ") || "no lines";

/**
 * Applies a standardized order set to the patient in one action — chains StartEncounter → ApplyOrderSet,
 * which fans out to the individual order commands (so each line runs the same safety checks). A blocking
 * advisory on any line surfaces an audited override step.
 */
export const OrderSetDialog = ({ patientId, onClose }: Props) => {
  const titleId = useId();
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<string | null>(null);
  const [overrideReason, setOverrideReason] = useState("");

  const sets = useQuery({ queryKey: ["ehr", "order-sets"], queryFn: fetchOrderSets });

  const apply = useMutation({
    mutationFn: async (vars: {
      acknowledge?: boolean;
      reason?: string;
    }): Promise<ApplyOrderSetResult> => {
      const encounterId = await startEncounter({
        patientId,
        providerId: DEMO_PROVIDER_ID,
        encounterClassCode: "AMB",
      });
      return applyOrderSet(selected as string, {
        patientId,
        encounterId,
        orderingProviderId: DEMO_PROVIDER_ID,
        acknowledgeAdvisories: vars.acknowledge,
        overrideReason: vars.reason,
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["ehr", "chart", patientId] });
      onClose();
    },
  });

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape" && !apply.isPending) onClose();
    };
    globalThis.addEventListener("keydown", handler);
    return () => globalThis.removeEventListener("keydown", handler);
  }, [apply.isPending, onClose]);

  const blocked = apply.error instanceof SafetyBlockedError ? apply.error.advisories : null;

  return createPortal(
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      onClick={() => {
        if (!apply.isPending) onClose();
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="w-full max-w-lg rounded-lg border border-slate-800 bg-slate-900 p-6 shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="mb-4">
          <p className="text-xs uppercase tracking-wide text-slate-400">Order set</p>
          <h2 id={titleId} className="text-lg font-semibold text-clinic-50">
            Apply a standardized order set
          </h2>
          <p className="text-xs text-slate-400">
            Files every line as a real order — same safety checks as ordering individually.
          </p>
        </header>

        {sets.isLoading && <p className="text-xs text-slate-400">Loading order sets…</p>}
        {sets.error && <p className="text-sm text-rose-300">{humanizeError(sets.error)}</p>}
        {sets.data && sets.data.length === 0 && (
          <p className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
            No order sets configured yet.
          </p>
        )}

        {sets.data && sets.data.length > 0 && (
          <ul className="space-y-2">
            {sets.data.map((s) => (
              <li key={s.id}>
                <button
                  type="button"
                  onClick={() => setSelected(s.id)}
                  className={`w-full rounded-md border px-3 py-2 text-left text-sm transition ${
                    selected === s.id
                      ? "border-clinic-500 bg-clinic-900/60 text-clinic-50"
                      : "border-slate-700 text-slate-200 hover:border-slate-500"
                  }`}
                >
                  <span className="font-medium">{s.name}</span>
                  <span className="ml-2 text-xs text-slate-400">{lineSummary(s)}</span>
                </button>
              </li>
            ))}
          </ul>
        )}

        {blocked && (
          <div className="mt-3 space-y-2 rounded-md border border-rose-700 bg-rose-950/40 p-3">
            <p className="text-xs font-semibold uppercase tracking-wide text-rose-200">
              Safety check — review before overriding
            </p>
            <SafetyAdvisoryList advisories={blocked} />
            <label className="block text-xs text-slate-300">
              Override reason (audited)
              <textarea
                value={overrideReason}
                onChange={(e) => setOverrideReason(e.target.value)}
                rows={2}
                className="mt-1 w-full rounded-md border border-slate-700 bg-slate-950 p-2 text-sm text-slate-100"
                placeholder="Why these orders are appropriate despite the advisory…"
              />
            </label>
          </div>
        )}

        {apply.error && !blocked && (
          <p role="alert" className="mt-3 text-sm text-rose-300">
            {humanizeError(apply.error)}
          </p>
        )}

        <div className="mt-4 flex items-center justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            disabled={apply.isPending}
            className="rounded-md border border-slate-700 px-3 py-1.5 text-sm text-slate-300 transition hover:border-slate-500 disabled:opacity-50"
          >
            Cancel
          </button>
          {blocked ? (
            <button
              type="button"
              onClick={() => apply.mutate({ acknowledge: true, reason: overrideReason.trim() })}
              disabled={apply.isPending || overrideReason.trim().length === 0}
              className="rounded-md bg-rose-600 px-4 py-1.5 text-sm font-medium text-white transition hover:bg-rose-500 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {apply.isPending ? "Applying…" : "Override & apply"}
            </button>
          ) : (
            <button
              type="button"
              onClick={() => apply.mutate({})}
              disabled={apply.isPending || !selected}
              className="rounded-md bg-clinic-600 px-4 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {apply.isPending ? "Applying…" : "Apply set"}
            </button>
          )}
        </div>
      </div>
    </div>,
    document.body,
  );
};
