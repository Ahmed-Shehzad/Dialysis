import { useEffect, useId, useMemo, useState } from "react";
import { createPortal } from "react-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  COMMON_LAB_PANELS,
  DEMO_LAB_FACILITY,
  DEMO_PROVIDER_ID,
  orderLabTest,
  startEncounter,
} from "@/features/ehr/api/ehrApi";
import { humanizeError } from "@/lib/api/humanizeError";

interface OrderLabsDialogProps {
  patientId: string;
  onClose(): void;
}

interface OrderLabsPayload {
  patientId: string;
  loincPanelCodes: readonly string[];
}

/**
 * Two-step server orchestration matching `AddNoteDialog`: a lab order requires an
 * Encounter, so the dialog starts an ambulatory encounter for the patient on submit
 * and then files the lab order inside it. Provider + lab facility are demo constants
 * until real auth-claim → provider mapping and a real lab directory exist.
 */
const submitOrder = async ({ patientId, loincPanelCodes }: OrderLabsPayload): Promise<string> => {
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
  });
};

/**
 * Focused dialog for ordering one or more lab panels from the EHR chart. Surfaces a
 * short list of dialysis-relevant LOINC panels as toggleable chips; submit chains
 * StartEncounter → OrderLabTest. Closes on success; humanizeError surfaces failures.
 */
export const OrderLabsDialog = ({ patientId, onClose }: OrderLabsDialogProps) => {
  const titleId = useId();
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<readonly string[]>([]);

  const mutation = useMutation({
    mutationFn: submitOrder,
    onSuccess: () => {
      // Future EHR chart "Orders" section will surface the new order; invalidating the
      // chart query now keeps that future change a one-liner.
      void queryClient.invalidateQueries({ queryKey: ["ehr", "chart", patientId] });
      onClose();
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

  const canSubmit = !mutation.isPending && selected.length > 0;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    mutation.mutate({ patientId, loincPanelCodes: selected });
  };

  // Pre-computed so re-renders during typing / selection don't re-allocate.
  const panels = useMemo(() => COMMON_LAB_PANELS, []);

  return createPortal(
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      onClick={() => {
        if (!mutation.isPending) onClose();
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
          <p className="text-xs uppercase tracking-wide text-slate-400">Order labs</p>
          <h2 id={titleId} className="text-lg font-semibold text-clinic-50">
            Pick one or more panels
          </h2>
          <p className="text-xs text-slate-400">
            Files into a fresh ambulatory encounter at {DEMO_LAB_FACILITY}.
          </p>
        </header>

        <form onSubmit={handleSubmit} className="space-y-4">
          <fieldset className="space-y-2">
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

          {mutation.error && (
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
            <button
              type="submit"
              disabled={!canSubmit}
              className="rounded-md bg-clinic-600 px-4 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {mutation.isPending ? "Sending order…" : "Send order"}
            </button>
          </div>
        </form>
      </div>
    </div>,
    document.body,
  );
};
