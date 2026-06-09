import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  fetchNeedsFollowUp,
  type HospitalEvent,
  hospitalEventKindLabel,
  markHospitalEventFollowedUp,
} from "@/features/care-coordination/api/careCoordinationApi";
import { humanizeError } from "@/lib/api/humanizeError";
import { PatientLabel } from "@/features/patients/PatientLabel";

const kindTone = (kind: HospitalEvent["kind"]): string => {
  switch (kind) {
    case "Discharged":
      return "text-amber-300";
    case "Admitted":
      return "text-sky-300";
    case "ExternalEncounter":
      return "text-violet-300";
  }
};

/**
 * Facility-wide hospital-event follow-up worklist — patients admitted, discharged, or seen at an outside
 * org. Working a row (marking it followed-up) drops it off the list. The "proactively follow up after a
 * hospital stay" surface.
 */
export const CareCoordinationWorklistPage = () => {
  const queryClient = useQueryClient();
  const worklist = useQuery({
    queryKey: ["ehr", "care-coordination", "needs-follow-up"],
    queryFn: () => fetchNeedsFollowUp(),
  });

  const followUp = useMutation({
    mutationFn: (id: string) => markHospitalEventFollowedUp(id),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["ehr", "care-coordination", "needs-follow-up"] }),
  });

  return (
    <div className="space-y-6">
      <header>
        <p className="text-xs uppercase tracking-wide text-slate-400">Care coordination</p>
        <h2 className="text-2xl font-semibold text-clinic-50">Hospital-event follow-up</h2>
        <p className="text-xs text-slate-400">
          Patients admitted, discharged, or seen elsewhere — follow up, then mark done.
        </p>
      </header>

      <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        {worklist.isLoading && <p className="text-xs text-slate-400">Loading…</p>}
        {worklist.error && <p className="text-xs text-rose-300">{humanizeError(worklist.error)}</p>}
        {followUp.error && <p className="text-xs text-rose-300">{humanizeError(followUp.error)}</p>}
        {worklist.data && worklist.data.length === 0 && (
          <p className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
            Nothing to follow up — clear.
          </p>
        )}
        {worklist.data && worklist.data.length > 0 && (
          <ul className="divide-y divide-slate-800 text-sm">
            {worklist.data.map((e) => (
              <li key={e.id} className="grid grid-cols-12 items-center gap-2 py-2">
                <span className={`col-span-2 text-xs font-semibold uppercase ${kindTone(e.kind)}`}>
                  {hospitalEventKindLabel(e.kind)}
                </span>
                <span className="col-span-3 truncate text-xs text-slate-300">
                  {e.patientId ? (
                    <PatientLabel patientId={e.patientId} showMrn={false} />
                  ) : (
                    <span className="font-mono text-amber-300" title={e.externalPatientRef ?? ""}>
                      unmatched ({e.externalPatientRef ?? "?"})
                    </span>
                  )}
                </span>
                <span className="col-span-3 text-slate-300">{e.detail ?? e.source}</span>
                <span className="col-span-2 text-xs text-slate-500">
                  {new Date(e.occurredAtUtc).toLocaleDateString()}
                </span>
                <span className="col-span-2 text-right">
                  <button
                    type="button"
                    onClick={() => followUp.mutate(e.id)}
                    disabled={followUp.isPending}
                    className="rounded-md border border-slate-700 px-2 py-1 text-xs text-emerald-300 transition hover:border-slate-500 disabled:opacity-50"
                  >
                    Mark done
                  </button>
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
};
