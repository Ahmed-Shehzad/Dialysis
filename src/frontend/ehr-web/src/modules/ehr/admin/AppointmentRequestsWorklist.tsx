import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  type AppointmentRequest,
  approveRequest,
  declineRequest,
  DEMO_PROVIDER_ID,
  fetchPendingRequests,
} from "@/features/appointment-requests/api/appointmentRequestsApi";
import { humanizeError } from "@/lib/api/humanizeError";

const worklistKey = ["ehr", "appointment-requests", "pending"];

const shortId = (id: string): string => id.slice(0, 8);

/**
 * Staff worklist of patient-submitted appointment requests. Approving books the real appointment (the
 * clinician picks the slot) and notifies the patient; declining records a note. Mirrors the Phase 8
 * care-coordination follow-up worklist.
 */
export const AppointmentRequestsWorklist = () => {
  const queryClient = useQueryClient();
  const [slot, setSlot] = useState<Record<string, string>>({});
  const [note, setNote] = useState<Record<string, string>>({});

  const worklist = useQuery({ queryKey: worklistKey, queryFn: () => fetchPendingRequests() });

  const approve = useMutation({
    mutationFn: (r: AppointmentRequest) => {
      const chosen = slot[r.id];
      const start = chosen ? new Date(chosen) : new Date(r.earliestPreferredUtc);
      const end = new Date(start.getTime() + 30 * 60_000);
      return approveRequest(r.id, {
        patientId: r.patientId,
        providerId: DEMO_PROVIDER_ID,
        startUtc: start.toISOString(),
        endUtc: end.toISOString(),
        encounterClassCode: "AMB",
        visitReason: r.reasonText,
        staffNote: note[r.id]?.trim() || undefined,
      });
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: worklistKey }),
  });

  const decline = useMutation({
    mutationFn: (r: AppointmentRequest) =>
      declineRequest(r.id, note[r.id]?.trim() || "Unable to accommodate"),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: worklistKey }),
  });

  return (
    <div className="space-y-6">
      <header>
        <p className="text-xs uppercase tracking-wide text-slate-400">Scheduling</p>
        <h2 className="text-2xl font-semibold text-clinic-50">Appointment requests</h2>
        <p className="text-xs text-slate-400">
          Patient-submitted requests — pick a slot to approve (books the visit) or decline with a
          note.
        </p>
      </header>

      {worklist.isLoading && <p className="text-sm text-slate-400">Loading requests…</p>}
      {worklist.error && (
        <p role="alert" className="text-sm text-rose-300">
          {humanizeError(worklist.error)}
        </p>
      )}
      {worklist.data && worklist.data.length === 0 && (
        <p className="rounded-md border border-dashed border-slate-700 p-4 text-sm text-slate-500">
          No pending appointment requests.
        </p>
      )}

      {worklist.data && worklist.data.length > 0 && (
        <ul className="space-y-3">
          {worklist.data.map((r) => (
            <li key={r.id} className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div>
                  <p className="text-sm text-slate-100">{r.reasonText}</p>
                  <p className="text-xs text-slate-500">
                    Patient {shortId(r.patientId)} · prefers{" "}
                    {new Date(r.earliestPreferredUtc).toLocaleDateString()} –{" "}
                    {new Date(r.latestPreferredUtc).toLocaleDateString()}
                  </p>
                </div>
              </div>

              <div className="mt-3 flex flex-wrap items-end gap-2">
                <label className="flex flex-col gap-1 text-xs text-slate-300">
                  Slot
                  <input
                    type="datetime-local"
                    value={slot[r.id] ?? r.earliestPreferredUtc.slice(0, 16)}
                    onChange={(e) => setSlot((s) => ({ ...s, [r.id]: e.target.value }))}
                    className="rounded-md border border-slate-700 bg-slate-950 px-2 py-1 text-slate-100"
                  />
                </label>
                <input
                  type="text"
                  value={note[r.id] ?? ""}
                  onChange={(e) => setNote((n) => ({ ...n, [r.id]: e.target.value }))}
                  placeholder="Staff note (optional for approve, used on decline)"
                  aria-label="Staff note"
                  className="flex-1 rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 text-sm text-slate-100"
                />
                <button
                  type="button"
                  onClick={() => approve.mutate(r)}
                  disabled={approve.isPending}
                  className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:opacity-50"
                >
                  Approve & book
                </button>
                <button
                  type="button"
                  onClick={() => decline.mutate(r)}
                  disabled={decline.isPending}
                  className="rounded-md border border-rose-700 px-3 py-1.5 text-sm text-rose-200 transition hover:bg-rose-950/40 disabled:opacity-50"
                >
                  Decline
                </button>
              </div>
              {(approve.error || decline.error) && (
                <p className="mt-2 text-xs text-rose-300">
                  {humanizeError(approve.error ?? decline.error)}
                </p>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};
