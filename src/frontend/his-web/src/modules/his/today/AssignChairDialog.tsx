import { useEffect, useId, useMemo, useState } from "react";
import { createPortal } from "react-dom";
import { humanizeError } from "@/lib/api/humanizeError";
import { usePatientContext } from "@/shell/PatientContextProvider";
import { freeChairs, useAssignChair, useTodaysQueue, type QueueEntry } from "./queueApi";

interface AssignChairDialogProps {
  entry: QueueEntry;
  onClose(): void;
}

/**
 * Focused dialog for the receptionist's chair-assignment step. The chair list is computed
 * from the live queue so already-occupied chairs aren't selectable. On submit the
 * optimistic mutation moves the card to In treatment with the chosen chair; on success
 * the patient is carried into the cross-module patient context for the nurse to pick up
 * in the EHR / PDMS modules.
 */
export const AssignChairDialog = ({ entry, onClose }: AssignChairDialogProps) => {
  const titleId = useId();
  const queue = useTodaysQueue();
  const assign = useAssignChair();
  const { select } = usePatientContext();

  const available = useMemo(() => freeChairs(queue.data ?? []), [queue.data]);
  const [chair, setChair] = useState<string>("");

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape" && !assign.isPending) onClose();
    };
    globalThis.addEventListener("keydown", handler);
    return () => globalThis.removeEventListener("keydown", handler);
  }, [assign.isPending, onClose]);

  const canSubmit = !assign.isPending && chair !== "";

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    assign.mutate(
      { entryId: entry.id, chair },
      {
        onSuccess: () => {
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
        if (e.target === e.currentTarget && !assign.isPending) onClose();
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="w-full max-w-md rounded-lg border border-slate-800 bg-slate-900 p-6 shadow-xl"
      >
        <header className="mb-4">
          <p className="text-xs uppercase tracking-wide text-slate-400">Assign chair</p>
          <h2 id={titleId} className="text-lg font-semibold text-clinic-50">
            {entry.patientName}
          </h2>
          <p className="text-xs text-slate-400">{entry.mrn}</p>
        </header>

        <form onSubmit={handleSubmit} className="space-y-4">
          {available.length === 0 ? (
            <p className="rounded-md border border-amber-700 bg-amber-900/30 p-3 text-sm text-amber-100">
              Every chair is in use right now. Wait for a patient to finish, or take this patient
              off the queue.
            </p>
          ) : (
            <fieldset className="space-y-2">
              <legend className="text-sm text-slate-300">Pick an open chair</legend>
              <div className="grid grid-cols-4 gap-2">
                {available.map((c) => (
                  <label
                    key={c}
                    className={`cursor-pointer rounded-md border px-2 py-2 text-center text-sm transition ${
                      chair === c
                        ? "border-clinic-500 bg-clinic-900/60 text-clinic-50"
                        : "border-slate-700 text-slate-300 hover:border-slate-500"
                    }`}
                  >
                    <input
                      type="radio"
                      name="chair"
                      value={c}
                      checked={chair === c}
                      onChange={() => setChair(c)}
                      className="sr-only"
                    />
                    {c.replace("Chair ", "#")}
                  </label>
                ))}
              </div>
            </fieldset>
          )}

          {assign.error && (
            <p role="alert" className="text-sm text-rose-300">
              {humanizeError(assign.error)}
            </p>
          )}

          <div className="flex items-center justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              disabled={assign.isPending}
              className="rounded-md border border-slate-700 px-3 py-1.5 text-sm text-slate-300 transition hover:border-slate-500 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={!canSubmit}
              autoFocus
              className="rounded-md bg-clinic-600 px-4 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {assign.isPending ? "Assigning…" : "Assign chair"}
            </button>
          </div>
        </form>
      </div>
    </div>,
    document.body,
  );
};
