import { useEffect, useId, useMemo, useState } from "react";
import { createPortal } from "react-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  COMMON_MEDICATIONS,
  DEMO_PHARMACY_NCPDP,
  DEMO_PROVIDER_ID,
  orderPrescription,
  type OrderResult,
  SafetyBlockedError,
  startEncounter,
} from "@/features/ehr/api/ehrApi";
import { humanizeError } from "@/lib/api/humanizeError";
import { SafetyAdvisoryList } from "@/modules/ehr/chart/SafetyAdvisoryList";

interface OrderPrescriptionDialogProps {
  patientId: string;
  onClose(): void;
}

interface PrescribePayload {
  patientId: string;
  rxnorm: string;
  display: string;
  doseText: string;
  frequencyText: string;
  quantityDispensed: number;
  refillsAuthorized: number;
  acknowledgeAdvisories?: boolean;
  overrideReason?: string;
}

/** Chains StartEncounter → OrderPrescription, like the labs dialog. */
const submitPrescription = async (p: PrescribePayload): Promise<OrderResult> => {
  const encounterId = await startEncounter({
    patientId: p.patientId,
    providerId: DEMO_PROVIDER_ID,
    encounterClassCode: "AMB",
  });
  return orderPrescription({
    patientId: p.patientId,
    encounterId,
    prescribingProviderId: DEMO_PROVIDER_ID,
    medicationRxnormCode: p.rxnorm,
    medicationDisplay: p.display,
    doseText: p.doseText,
    frequencyText: p.frequencyText,
    quantityDispensed: p.quantityDispensed,
    refillsAuthorized: p.refillsAuthorized,
    pharmacyNcpdpId: DEMO_PHARMACY_NCPDP,
    acknowledgeAdvisories: p.acknowledgeAdvisories,
    overrideReason: p.overrideReason,
  });
};

/**
 * Prescribe dialog. Submit runs point-of-care safety checks server-side: a medication↔allergy
 * conflict returns a blocking advisory (HTTP 422) and the dialog requires an audited override
 * reason before re-submitting; duplicate-medication advisories are shown but non-blocking.
 */
export const OrderPrescriptionDialog = ({ patientId, onClose }: OrderPrescriptionDialogProps) => {
  const titleId = useId();
  const queryClient = useQueryClient();
  const [rxnorm, setRxnorm] = useState(COMMON_MEDICATIONS[0]?.rxnorm ?? "");
  const [doseText, setDoseText] = useState("1 tablet");
  const [frequencyText, setFrequencyText] = useState("once daily");
  const [quantity, setQuantity] = useState(30);
  const [refills, setRefills] = useState(3);
  const [overrideReason, setOverrideReason] = useState("");

  const medication = useMemo(
    () =>
      COMMON_MEDICATIONS.find((m) => m.rxnorm === rxnorm) ??
      COMMON_MEDICATIONS[0] ?? { rxnorm, display: rxnorm },
    [rxnorm],
  );

  const mutation = useMutation({
    mutationFn: submitPrescription,
    onSuccess: (result) => {
      void queryClient.invalidateQueries({ queryKey: ["ehr", "chart", patientId] });
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

  const blocked = mutation.error instanceof SafetyBlockedError ? mutation.error.advisories : null;
  const placed = mutation.data && mutation.data.advisories.length > 0 ? mutation.data : null;

  const basePayload = (): PrescribePayload => ({
    patientId,
    rxnorm: medication.rxnorm,
    display: medication.display,
    doseText: doseText.trim(),
    frequencyText: frequencyText.trim(),
    quantityDispensed: quantity,
    refillsAuthorized: refills,
  });

  const canSubmit =
    !mutation.isPending &&
    doseText.trim().length > 0 &&
    frequencyText.trim().length > 0 &&
    quantity > 0;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    mutation.mutate(basePayload());
  };

  const handleOverride = () => {
    if (mutation.isPending || overrideReason.trim().length === 0) return;
    mutation.mutate({
      ...basePayload(),
      acknowledgeAdvisories: true,
      overrideReason: overrideReason.trim(),
    });
  };

  const fieldClass =
    "mt-1 w-full rounded-md border border-slate-700 bg-slate-950 p-2 text-sm text-slate-100";

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
          <p className="text-xs uppercase tracking-wide text-slate-400">Prescribe</p>
          <h2 id={titleId} className="text-lg font-semibold text-clinic-50">
            New prescription
          </h2>
          <p className="text-xs text-slate-400">
            Checked against the chart for allergy conflicts and duplicates before it&apos;s filed.
          </p>
        </header>

        {placed ? (
          <div className="space-y-4">
            <p className="text-sm text-emerald-300">Prescription filed. Please note:</p>
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
          <form onSubmit={handleSubmit} className="space-y-3">
            <fieldset className="space-y-3" disabled={mutation.isPending}>
              <legend className="sr-only">Prescription</legend>
              <label className="block text-xs text-slate-300">
                Medication
                <select
                  value={rxnorm}
                  onChange={(e) => setRxnorm(e.target.value)}
                  className={fieldClass}
                >
                  {COMMON_MEDICATIONS.map((m) => (
                    <option key={m.rxnorm} value={m.rxnorm}>
                      {m.display}
                    </option>
                  ))}
                </select>
              </label>
              <div className="grid grid-cols-2 gap-3">
                <label className="block text-xs text-slate-300">
                  Dose
                  <input
                    value={doseText}
                    onChange={(e) => setDoseText(e.target.value)}
                    className={fieldClass}
                  />
                </label>
                <label className="block text-xs text-slate-300">
                  Frequency
                  <input
                    value={frequencyText}
                    onChange={(e) => setFrequencyText(e.target.value)}
                    className={fieldClass}
                  />
                </label>
                <label className="block text-xs text-slate-300">
                  Quantity
                  <input
                    type="number"
                    min={1}
                    value={quantity}
                    onChange={(e) => setQuantity(Number(e.target.value))}
                    className={fieldClass}
                  />
                </label>
                <label className="block text-xs text-slate-300">
                  Refills
                  <input
                    type="number"
                    min={0}
                    value={refills}
                    onChange={(e) => setRefills(Number(e.target.value))}
                    className={fieldClass}
                  />
                </label>
              </div>
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
                    className={fieldClass}
                    placeholder="Why this medication is clinically appropriate despite the allergy…"
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
                  {mutation.isPending ? "Overriding…" : "Override & prescribe"}
                </button>
              ) : (
                <button
                  type="submit"
                  disabled={!canSubmit}
                  className="rounded-md bg-clinic-600 px-4 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {mutation.isPending ? "Prescribing…" : "Prescribe"}
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
