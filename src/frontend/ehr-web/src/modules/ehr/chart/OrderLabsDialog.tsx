import { useEffect, useId, useMemo, useState } from "react";
import { createPortal } from "react-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  COMMON_LAB_PANELS,
  DEMO_LAB_FACILITY,
  DEMO_PROVIDER_ID,
  orderLabTest,
  type OrderResult,
  SafetyBlockedError,
  startEncounter,
} from "@/features/ehr/api/ehrApi";
import { humanizeError } from "@/lib/api/humanizeError";
import { SafetyAdvisoryList } from "@/modules/ehr/chart/SafetyAdvisoryList";

interface OrderLabsDialogProps {
  patientId: string;
  onClose(): void;
}

interface OrderLabsPayload {
  patientId: string;
  loincPanelCodes: readonly string[];
  acknowledgeAdvisories?: boolean;
  overrideReason?: string;
}

/**
 * Two-step server orchestration matching `AddNoteDialog`: a lab order requires an
 * Encounter, so the dialog starts an ambulatory encounter for the patient on submit
 * and then files the lab order inside it. Provider + lab facility are demo constants
 * until real auth-claim → provider mapping and a real lab directory exist.
 */
const submitOrder = async ({
  patientId,
  loincPanelCodes,
  acknowledgeAdvisories,
  overrideReason,
}: OrderLabsPayload): Promise<OrderResult> => {
  const encounterId = await startEncounter({
    patientId,
    providerId: DEMO_PROVIDER_ID,
    encounterClassCode: "AMB",
  });
  return orderLabTest({
    patientId,
    encounterId,
    orderingProviderId: DEMO_PROVIDER_ID,
    labFacilityCode: DEMO_LAB_FACILITY,
    loincPanelCodes: [...loincPanelCodes],
    acknowledgeAdvisories,
    overrideReason,
  });
};

/**
 * Focused dialog for ordering one or more lab panels from the EHR chart. Surfaces a
 * short list of dialysis-relevant LOINC panels as toggleable chips; submit chains
 * StartEncounter → OrderLabTest. Duplicate-order advisories (non-blocking) are shown after
 * ordering; a blocking advisory (if ever configured) presents an audited override step.
 */
export const OrderLabsDialog = ({ patientId, onClose }: OrderLabsDialogProps) => {
  const titleId = useId();
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<readonly string[]>([]);
  const [overrideReason, setOverrideReason] = useState("");

  const mutation = useMutation({
    mutationFn: submitOrder,
    onSuccess: (result) => {
      void queryClient.invalidateQueries({ queryKey: ["ehr", "chart", patientId] });
      // Close immediately when nothing needs the clinician's attention; otherwise keep the
      // dialog open to surface the advisory before they move on.
      if (result.advisories.length === 0) onClose();
    },
  });

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape" && !mutation.isPending) onClose();
    };
    globalThis.addEventListener("keydown", handler);
    return () => globalThis.removeEventListener("keydown", handler);
  }, [mutation.isPending, onClose]);

  const toggle = (loinc: string) =>
    setSelected((prev) =>
      prev.includes(loinc) ? prev.filter((c) => c !== loinc) : [...prev, loinc],
    );

  const blocked = mutation.error instanceof SafetyBlockedError ? mutation.error.advisories : null;
  const placed = mutation.data && mutation.data.advisories.length > 0 ? mutation.data : null;

  const canSubmit = !mutation.isPending && selected.length > 0;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    mutation.mutate({ patientId, loincPanelCodes: selected });
  };

  const handleOverride = () => {
    if (mutation.isPending || overrideReason.trim().length === 0) return;
    mutation.mutate({
      patientId,
      loincPanelCodes: selected,
      acknowledgeAdvisories: true,
      overrideReason: overrideReason.trim(),
    });
  };

  // Pre-computed so re-renders during typing / selection don't re-allocate.
  const panels = useMemo(() => COMMON_LAB_PANELS, []);

  return createPortal(
    <div
      role="presentation"
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      onClick={(e) => {
        // Backdrop-only dismissal — clicks inside the dialog bubble here with a
        // different target, so they never close it. Escape (wired above) is the
        // keyboard dismissal path.
        if (e.target === e.currentTarget && !mutation.isPending) onClose();
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="w-full max-w-lg rounded-lg border border-slate-800 bg-slate-900 p-6 shadow-xl"
      >
        <header className="mb-4">
          <p className="text-xs uppercase tracking-wide text-slate-400">Order labs</p>
          <h2 id={titleId} className="text-lg font-semibold text-clinic-50">
            Pick one or more panels
          </h2>
          <p className="text-xs text-slate-400">
            Files into a fresh ambulatory encounter at {DEMO_LAB_FACILITY}.
          </p>
        </header>

        {placed ? (
          <div className="space-y-4">
            <p className="text-sm text-emerald-300">Order placed. Please note:</p>
            <SafetyAdvisoryList advisories={placed.advisories} />
            <div className="flex justify-end pt-2">
              <button
                type="button"
                onClick={onClose}
                className="rounded-md bg-clinic-600 px-4 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500"
              >
                Done
              </button>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            <fieldset className="space-y-2" disabled={mutation.isPending}>
              <legend className="sr-only">Lab panels</legend>
              <div className="flex flex-wrap gap-2">
                {panels.map((p) => {
                  const active = selected.includes(p.loinc);
                  return (
                    <button
                      key={p.loinc}
                      type="button"
                      onClick={() => toggle(p.loinc)}
                      aria-pressed={active}
                      title={p.loinc}
                      className={`rounded-full border px-3 py-1.5 text-sm transition ${
                        active
                          ? "border-clinic-500 bg-clinic-900/60 text-clinic-50"
                          : "border-slate-700 text-slate-300 hover:border-slate-500"
                      }`}
                    >
                      {p.display}
                    </button>
                  );
                })}
              </div>
              <p className="text-xs text-slate-500">
                {selected.length === 0
                  ? "Select at least one panel."
                  : `${selected.length} selected.`}
              </p>
            </fieldset>

            {blocked && (
              <div className="space-y-2 rounded-md border border-rose-700 bg-rose-950/40 p-3">
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
                    placeholder="Why this order is clinically appropriate despite the advisory…"
                  />
                </label>
              </div>
            )}

            {mutation.error && !blocked && (
              <p role="alert" className="text-sm text-rose-300">
                {humanizeError(mutation.error)}
              </p>
            )}

            <div className="flex items-center justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={onClose}
                disabled={mutation.isPending}
                className="rounded-md border border-slate-700 px-3 py-1.5 text-sm text-slate-300 transition hover:border-slate-500 disabled:opacity-50"
              >
                Cancel
              </button>
              {blocked ? (
                <button
                  type="button"
                  onClick={handleOverride}
                  disabled={mutation.isPending || overrideReason.trim().length === 0}
                  className="rounded-md bg-rose-600 px-4 py-1.5 text-sm font-medium text-white transition hover:bg-rose-500 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {mutation.isPending ? "Overriding…" : "Override & order"}
                </button>
              ) : (
                <button
                  type="submit"
                  disabled={!canSubmit}
                  className="rounded-md bg-clinic-600 px-4 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {mutation.isPending ? "Sending order…" : "Send order"}
                </button>
              )}
            </div>
          </form>
        )}
      </div>
    </div>,
    document.body,
  );
};
