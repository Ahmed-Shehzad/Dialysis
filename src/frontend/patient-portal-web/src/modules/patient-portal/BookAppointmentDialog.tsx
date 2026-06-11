import { useEffect, useId, useMemo, useState } from "react";
import { createPortal } from "react-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { bookAppointment, DEMO_HIS_PROVIDER_ID } from "@/features/scheduling/api";
import { humanizeError } from "@/lib/api/humanizeError";

interface BookAppointmentDialogProps {
  patientId: string;
  onClose(): void;
}

const DEFAULT_DURATION_MIN = 30;

const todayPlusDaysIso = (days: number): string => {
  const d = new Date();
  d.setDate(d.getDate() + days);
  // YYYY-MM-DD for <input type="date">
  return d.toISOString().slice(0, 10);
};

const composeIsoUtc = (dateYmd: string, timeHm: string): string => {
  // <input type="date"> + <input type="time"> are local-time; build a Date in local tz
  // and let toISOString convert to UTC. Avoids the "Z parsed as local" mojibake.
  const [y, m, d] = dateYmd.split("-").map(Number);
  const [h, min] = timeHm.split(":").map(Number);
  if (!y || !m || !d || h === undefined || min === undefined) return "";
  const local = new Date(y, m - 1, d, h, min, 0, 0);
  return local.toISOString();
};

/**
 * Patient-portal appointment booking dialog. Captures a date, a start time, and an
 * (optional) duration in minutes; submits to HIS `POST /api/v1.0/scheduling/appointments`
 * via the demo provider Guid (the validator only enforces ProviderId != Empty — a real
 * provider directory + slot lookup is the next slice).
 *
 * On success, invalidates the portal summary so the upcoming-appointments tile increments
 * immediately, and closes the dialog. Cancel and Esc close without booking.
 */
export const BookAppointmentDialog = ({ patientId, onClose }: BookAppointmentDialogProps) => {
  const titleId = useId();
  const queryClient = useQueryClient();
  const [date, setDate] = useState(() => todayPlusDaysIso(1));
  const [startTime, setStartTime] = useState("09:00");
  const [durationMin, setDurationMin] = useState(DEFAULT_DURATION_MIN);

  const slot = useMemo(() => {
    const start = composeIsoUtc(date, startTime);
    if (!start) return null;
    const startMs = new Date(start).getTime();
    const endMs = startMs + durationMin * 60_000;
    return { start, end: new Date(endMs).toISOString() };
  }, [date, startTime, durationMin]);

  const mutation = useMutation({
    mutationFn: () => {
      if (!slot) throw new Error("Pick a date and start time first.");
      return bookAppointment({
        patientId,
        providerId: DEMO_HIS_PROVIDER_ID,
        slotStartUtc: slot.start,
        slotEndUtc: slot.end,
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["patient-portal", "summary", patientId] });
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

  const canSubmit = !mutation.isPending && slot !== null && durationMin > 0;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    mutation.mutate();
  };

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
        className="w-full max-w-md rounded-lg border border-slate-800 bg-slate-900 p-6 shadow-xl"
      >
        <header className="mb-4">
          <p className="text-xs uppercase tracking-wide text-slate-400">Book appointment</p>
          <h2 id={titleId} className="text-lg font-semibold text-clinic-50">
            Request a clinic visit
          </h2>
          <p className="text-xs text-slate-400">
            The clinic will confirm by phone or message — your booking is pending until then.
          </p>
        </header>

        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="grid gap-3 sm:grid-cols-2">
            <label className="block text-sm">
              <span className="mb-1 block text-slate-300">Date</span>
              <input
                type="date"
                required
                min={todayPlusDaysIso(0)}
                value={date}
                onChange={(e) => setDate(e.target.value)}
                className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 focus:border-clinic-500 focus:outline-hidden"
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block text-slate-300">Start time</span>
              <input
                type="time"
                required
                value={startTime}
                onChange={(e) => setStartTime(e.target.value)}
                className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 focus:border-clinic-500 focus:outline-hidden"
              />
            </label>
          </div>

          <label className="block text-sm">
            <span className="mb-1 block text-slate-300">Duration (minutes)</span>
            <select
              value={durationMin}
              onChange={(e) => setDurationMin(Number.parseInt(e.target.value, 10))}
              className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 focus:border-clinic-500 focus:outline-hidden"
            >
              <option value={15}>15 minutes</option>
              <option value={30}>30 minutes</option>
              <option value={45}>45 minutes</option>
              <option value={60}>60 minutes</option>
              <option value={90}>90 minutes</option>
            </select>
          </label>

          {slot && (
            <p className="rounded-md border border-slate-700 bg-slate-950/60 p-2 text-xs text-slate-300">
              Booking {new Date(slot.start).toLocaleString()} →{" "}
              {new Date(slot.end).toLocaleTimeString()} (UTC)
            </p>
          )}

          {mutation.error && (
            <p role="alert" className="text-sm text-rose-300">
              {humanizeError(mutation.error)}
            </p>
          )}

          <div className="flex items-center justify-end gap-2 pt-1">
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
              {mutation.isPending ? "Booking…" : "Book appointment"}
            </button>
          </div>
        </form>
      </div>
    </div>,
    document.body,
  );
};
