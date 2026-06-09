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

type RollbackCtx = { previous?: AppointmentRequest[] };

// A 409 here is one of two things: the request was already handled (a stale/duplicate row — the refetch
// drops it), or the chosen slot is already booked (the row stays — pick another time). The generic
// "someone changed this — refresh" is misleading for both, so we say what actually happened.
const messageFor = (error: unknown): string => {
  const status = (error as { response?: { status?: number } })?.response?.status;
  if (status === 409) {
    return "Couldn't complete this — the request may already be handled, or that slot is taken. The list refreshed; if the row is still here, pick a different time.";
  }
  return humanizeError(error);
};

/**
 * Staff worklist of patient-submitted appointment requests. Approving books the real appointment (the
 * clinician picks the slot) and notifies the patient; declining records a note. Mirrors the Phase 8
 * care-coordination follow-up worklist.
 *
 * Acting on a row is optimistic + self-healing: the row is removed immediately, restored if the action
 * fails, and the list is always refetched on settle so it reflects server truth. The acted-on row is
 * locked while in flight so it can't be double-submitted (the race that produced the approve/decline 409s).
 */
export const AppointmentRequestsWorklist = () => {
  const queryClient = useQueryClient();
  const [slot, setSlot] = useState<Record<string, string>>({});
  const [note, setNote] = useState<Record<string, string>>({});
  // The row currently being approved/declined — locks BOTH of its buttons so it can't be re-submitted.
  const [busyId, setBusyId] = useState<string | null>(null);
  // Error scoped to the row that failed (the shared mutation hooks can't say which row on their own).
  const [rowError, setRowError] = useState<{ id: string; message: string } | null>(null);

  const worklist = useQuery({ queryKey: worklistKey, queryFn: () => fetchPendingRequests() });

  const onMutate = async (r: AppointmentRequest): Promise<RollbackCtx> => {
    setBusyId(r.id);
    setRowError(null);
    await queryClient.cancelQueries({ queryKey: worklistKey });
    const previous = queryClient.getQueryData<AppointmentRequest[]>(worklistKey);
    queryClient.setQueryData<AppointmentRequest[]>(worklistKey, (rows) =>
      (rows ?? []).filter((row) => row.id !== r.id),
    );
    return { previous };
  };

  const onError = (error: unknown, r: AppointmentRequest, ctx?: RollbackCtx) => {
    if (ctx?.previous) queryClient.setQueryData(worklistKey, ctx.previous);
    setRowError({ id: r.id, message: messageFor(error) });
  };

  const onSettled = () => {
    setBusyId(null);
    void queryClient.invalidateQueries({ queryKey: worklistKey });
  };

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
    onMutate,
    onError,
    onSettled,
  });

  const decline = useMutation({
    mutationFn: (r: AppointmentRequest) =>
      declineRequest(r.id, note[r.id]?.trim() || "Unable to accommodate"),
    onMutate,
    onError,
    onSettled,
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
          {worklist.data.map((r) => {
            const busy = busyId === r.id;
            return (
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
                    disabled={busy}
                    className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:opacity-50"
                  >
                    {busy ? "Working…" : "Approve & book"}
                  </button>
                  <button
                    type="button"
                    onClick={() => decline.mutate(r)}
                    disabled={busy}
                    className="rounded-md border border-rose-700 px-3 py-1.5 text-sm text-rose-200 transition hover:bg-rose-950/40 disabled:opacity-50"
                  >
                    Decline
                  </button>
                </div>
                {rowError?.id === r.id && (
                  <p role="alert" className="mt-2 text-xs text-rose-300">
                    {rowError.message}
                  </p>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
};
