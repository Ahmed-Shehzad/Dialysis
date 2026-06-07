import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { notify } from "@/features/durable-commands";
import {
  type AppointmentRequest,
  cancelRequest,
  fetchMyRequests,
  requestAppointment,
} from "@/features/appointment-requests/api/appointmentRequestsApi";
import { humanizeError } from "@/lib/api/humanizeError";

const requestsKey = (patientId: string) => ["patient-portal", "appointment-requests", patientId];

const statusTone = (status: string): string => {
  switch (status) {
    case "Approved":
      return "text-emerald-300";
    case "Declined":
      return "text-rose-300";
    case "Cancelled":
      return "text-slate-400";
    default:
      return "text-amber-300";
  }
};

const toLocalInput = (daysFromNow: number): string => {
  const d = new Date(Date.now() + daysFromNow * 86_400_000);
  return d.toISOString().slice(0, 10);
};

/**
 * Patient self-service appointment requests: submit a request, watch its status (Pending → Approved /
 * Declined), and cancel a still-pending one. Staff work the request from the clinician worklist; the
 * decision arrives as a real-time portal toast.
 */
export const MyAppointmentRequestsPanel = ({ patientId }: { patientId: string }) => {
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [earliest, setEarliest] = useState(toLocalInput(2));
  const [latest, setLatest] = useState(toLocalInput(9));

  const requests = useQuery({
    queryKey: requestsKey(patientId),
    queryFn: () => fetchMyRequests(patientId),
  });

  const submit = useMutation({
    mutationFn: () =>
      requestAppointment(patientId, {
        reasonText: reason.trim(),
        earliestPreferredUtc: new Date(`${earliest}T09:00:00Z`).toISOString(),
        latestPreferredUtc: new Date(`${latest}T17:00:00Z`).toISOString(),
      }),
    onSuccess: () => {
      notify({ kind: "success", message: "Appointment request sent." });
      setOpen(false);
      setReason("");
      void queryClient.invalidateQueries({ queryKey: requestsKey(patientId) });
    },
  });

  const cancel = useMutation({
    mutationFn: (requestId: string) => cancelRequest(patientId, requestId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: requestsKey(patientId) }),
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header className="flex items-center justify-between">
        <div>
          <h3 className="text-sm font-medium text-slate-200">Appointment requests</h3>
          <p className="text-xs text-slate-400">
            Ask the clinic for a visit — they&apos;ll confirm a time.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          className="rounded-md border border-slate-700 px-2.5 py-1 text-xs text-slate-200 transition hover:border-slate-500"
        >
          {open ? "Cancel" : "+ Request a visit"}
        </button>
      </header>

      {open && (
        <div className="space-y-2 rounded-md border border-slate-700 bg-slate-950/40 p-3">
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            rows={2}
            placeholder="Reason for the visit"
            aria-label="Reason for the visit"
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 text-sm text-slate-100"
          />
          <div className="flex flex-wrap gap-3 text-xs text-slate-300">
            <label className="flex flex-col gap-1">
              Earliest
              <input
                type="date"
                value={earliest}
                onChange={(e) => setEarliest(e.target.value)}
                className="rounded-md border border-slate-700 bg-slate-950 px-2 py-1 text-slate-100"
              />
            </label>
            <label className="flex flex-col gap-1">
              Latest
              <input
                type="date"
                value={latest}
                onChange={(e) => setLatest(e.target.value)}
                className="rounded-md border border-slate-700 bg-slate-950 px-2 py-1 text-slate-100"
              />
            </label>
          </div>
          {submit.error && <p className="text-xs text-rose-300">{humanizeError(submit.error)}</p>}
          <button
            type="button"
            onClick={() => submit.mutate()}
            disabled={submit.isPending || reason.trim().length === 0}
            className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {submit.isPending ? "Sending…" : "Send request"}
          </button>
        </div>
      )}

      {requests.isLoading && <p className="text-xs text-slate-400">Loading your requests…</p>}
      {requests.error && <p className="text-xs text-rose-300">{humanizeError(requests.error)}</p>}
      {requests.data && requests.data.length === 0 && !open && (
        <p className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
          No appointment requests yet.
        </p>
      )}

      {requests.data && requests.data.length > 0 && (
        <ul className="divide-y divide-slate-800 text-sm">
          {requests.data.map((r: AppointmentRequest) => (
            <li key={r.id} className="flex items-start justify-between gap-2 py-2">
              <div>
                <p className="text-slate-200">{r.reasonText}</p>
                <p className="text-xs text-slate-500">
                  {new Date(r.earliestPreferredUtc).toLocaleDateString()} –{" "}
                  {new Date(r.latestPreferredUtc).toLocaleDateString()}
                </p>
                {r.staffNote && <p className="text-xs text-slate-400">Note: {r.staffNote}</p>}
              </div>
              <div className="flex flex-col items-end gap-1">
                <span className={`text-xs ${statusTone(r.status)}`}>{r.status}</span>
                {r.status === "Pending" && (
                  <button
                    type="button"
                    onClick={() => cancel.mutate(r.id)}
                    disabled={cancel.isPending}
                    className="text-xs text-slate-400 hover:text-rose-300 disabled:opacity-50"
                  >
                    Cancel
                  </button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
};
