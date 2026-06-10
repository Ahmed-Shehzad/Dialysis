import { useEffect, useId, useState } from "react";
import { createPortal } from "react-dom";
import { humanizeError } from "@/lib/api/humanizeError";
import { usePatientContext } from "@/shell/PatientContextProvider";
import { useCheckInPatient, type QueueEntry } from "./queueApi";

interface CheckInDialogProps {
  entry: QueueEntry;
  onClose(): void;
}

/** "HH:mm" form value → ISO timestamp anchored to today. */
const localTimeToIso = (hhmm: string): string => {
  const [h, m] = hhmm.split(":").map(Number);
  const d = new Date();
  d.setHours(h ?? 0, m ?? 0, 0, 0);
  return d.toISOString();
};

const nowHhMm = (): string => {
  const d = new Date();
  const pad = (n: number) => n.toString().padStart(2, "0");
  return `${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

/**
 * Focused dialog for the receptionist's check-in step. Captures arrival time and, when
 * the patient's insurance wasn't pre-verified, an explicit acknowledgement that it was
 * checked at the counter. On submit the optimistic mutation moves the card to Waiting;
 * the dialog closes only after the server confirms (so failures keep the form open with
 * the error surfaced).
 */
export const CheckInDialog = ({ entry, onClose }: CheckInDialogProps) => {
  const titleId = useId();
  const checkIn = useCheckInPatient();
  const { select } = usePatientContext();

  const [arrival, setArrival] = useState<string>(nowHhMm());
  const [eligibilityAck, setEligibilityAck] = useState<boolean>(false);

  // Esc closes the dialog. We only attach the listener while the dialog is mounted.
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape" && !checkIn.isPending) onClose();
    };
    globalThis.addEventListener("keydown", handler);
    return () => globalThis.removeEventListener("keydown", handler);
  }, [checkIn.isPending, onClose]);

  const needsEligibilityAck = !entry.eligibilityVerified;
  const canSubmit = !checkIn.isPending && (!needsEligibilityAck || eligibilityAck);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    checkIn.mutate(
      {
        entryId: entry.id,
        arrivalTime: localTimeToIso(arrival),
        eligibilityAcknowledged: eligibilityAck,
      },
      {
        onSuccess: () => {
          // Carry the patient through to the rest of the shell — once checked in they're
          // the active patient until the receptionist clears them.
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
        if (e.target === e.currentTarget && !checkIn.isPending) onClose();
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="w-full max-w-md rounded-lg border border-slate-800 bg-slate-900 p-6 shadow-xl"
      >
        <header className="mb-4">
          <p className="text-xs uppercase tracking-wide text-slate-400">Check in</p>
          <h2 id={titleId} className="text-lg font-semibold text-clinic-50">
            {entry.patientName}
          </h2>
          <p className="text-xs text-slate-400">{entry.mrn}</p>
        </header>

        <form onSubmit={handleSubmit} className="space-y-4">
          <label className="block text-sm">
            <span className="mb-1 block text-slate-300">Arrival time</span>
            <input
              type="time"
              required
              value={arrival}
              onChange={(e) => setArrival(e.target.value)}
              className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100 focus:border-clinic-500 focus:outline-none"
            />
          </label>

          {needsEligibilityAck ? (
            <label className="flex items-start gap-2 rounded-md border border-amber-700 bg-amber-900/30 p-3 text-sm text-amber-100">
              <input
                type="checkbox"
                required
                checked={eligibilityAck}
                onChange={(e) => setEligibilityAck(e.target.checked)}
                className="mt-1"
              />
              <span>Insurance was not pre-verified. I confirmed eligibility at the counter.</span>
            </label>
          ) : (
            <p className="rounded-md border border-emerald-700/60 bg-emerald-900/20 p-3 text-sm text-emerald-200">
              Insurance verified — no further action needed.
            </p>
          )}

          {checkIn.error && (
            <p role="alert" className="text-sm text-rose-300">
              {humanizeError(checkIn.error)}
            </p>
          )}

          <div className="flex items-center justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              disabled={checkIn.isPending}
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
              {checkIn.isPending ? "Checking in…" : "Check in"}
            </button>
          </div>
        </form>
      </div>
    </div>,
    document.body,
  );
};
