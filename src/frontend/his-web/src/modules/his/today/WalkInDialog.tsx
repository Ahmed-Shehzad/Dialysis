import { useEffect, useId, useState } from "react";
import { createPortal } from "react-dom";
import { humanizeError } from "@/lib/api/humanizeError";
import { usePatientContext } from "@/shell/PatientContextProvider";
import { useRegisterWalkIn } from "./queueApi";

interface WalkInDialogProps {
  onClose(): void;
}

/**
 * Focused dialog for unannounced arrivals. Captures the minimum identifying information
 * (name + MRN) and whether insurance was confirmed at the counter. The patient is placed
 * directly into the Waiting column — walk-ins never appear under Expected because there
 * was no appointment to expect.
 */
export const WalkInDialog = ({ onClose }: WalkInDialogProps) => {
  const titleId = useId();
  const register = useRegisterWalkIn();
  const { select } = usePatientContext();

  const [patientName, setPatientName] = useState("");
  const [mrn, setMrn] = useState("");
  const [eligibilityVerified, setEligibilityVerified] = useState(false);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape" && !register.isPending) onClose();
    };
    globalThis.addEventListener("keydown", handler);
    return () => globalThis.removeEventListener("keydown", handler);
  }, [register.isPending, onClose]);

  const canSubmit = !register.isPending && patientName.trim() !== "" && mrn.trim() !== "";

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    register.mutate(
      { patientName, mrn, eligibilityVerified },
      {
        onSuccess: (entry) => {
          select({
            id: entry.patientId,
            displayName: entry.patientName,
            mrn: entry.mrn,
          });
          onClose();
        },
      },
    );
  };

  return createPortal(
    <div
      role="presentation"
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      onClick={(e) => {
        // Backdrop-only dismissal — clicks inside the dialog bubble here with a
        // different target, so they never close it. Escape (wired above) is the
        // keyboard dismissal path.
        if (e.target === e.currentTarget && !register.isPending) onClose();
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="w-full max-w-md rounded-lg border border-slate-800 bg-slate-900 p-6 shadow-xl"
      >
        <header className="mb-4">
          <p className="text-xs uppercase tracking-wide text-slate-400">Walk-in</p>
          <h2 id={titleId} className="text-lg font-semibold text-clinic-50">
            Register a new arrival
          </h2>
          <p className="text-xs text-slate-400">
            Adds the patient to the Waiting queue right away.
          </p>
        </header>

        <form onSubmit={handleSubmit} className="space-y-4">
          <label className="block text-sm">
            <span className="mb-1 block text-slate-300">Patient name</span>
            <input
              type="text"
              required
              autoFocus
              value={patientName}
              onChange={(e) => setPatientName(e.target.value)}
              placeholder="First Last"
              className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100 focus:border-clinic-500 focus:outline-none"
            />
          </label>

          <label className="block text-sm">
            <span className="mb-1 block text-slate-300">Medical record number</span>
            <input
              type="text"
              required
              value={mrn}
              onChange={(e) => setMrn(e.target.value)}
              placeholder="MRN-…"
              className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100 focus:border-clinic-500 focus:outline-none"
            />
          </label>

          <label className="flex items-start gap-2 text-sm text-slate-300">
            <input
              type="checkbox"
              checked={eligibilityVerified}
              onChange={(e) => setEligibilityVerified(e.target.checked)}
              className="mt-1"
            />
            <span>I confirmed insurance eligibility at the counter.</span>
          </label>

          {register.error && (
            <p role="alert" className="text-sm text-rose-300">
              {humanizeError(register.error)}
            </p>
          )}

          <div className="flex items-center justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              disabled={register.isPending}
              className="rounded-md border border-slate-700 px-3 py-1.5 text-sm text-slate-300 transition hover:border-slate-500 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={!canSubmit}
              className="rounded-md bg-clinic-600 px-4 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {register.isPending ? "Adding…" : "Add to Waiting"}
            </button>
          </div>
        </form>
      </div>
    </div>,
    document.body,
  );
};
