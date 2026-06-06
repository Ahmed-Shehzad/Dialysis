import { useQuery } from "@tanstack/react-query";
import {
  fetchPatientHospitalEvents,
  type HospitalEvent,
  hospitalEventKindLabel,
} from "@/features/care-coordination/api/careCoordinationApi";

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
 * Per-patient hospital events (admissions, discharges, outside-org encounters) — the care-team's
 * "has this patient been in the hospital?" view. Renders nothing when there are none.
 */
export const HospitalEventsCard = ({ patientId }: { patientId: string }) => {
  const events = useQuery({
    queryKey: ["ehr", "hospital-events", patientId],
    queryFn: () => fetchPatientHospitalEvents(patientId),
    enabled: Boolean(patientId),
  });

  if (!events.data || events.data.length === 0) return null;

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <h3 className="mb-2 text-sm font-medium text-slate-200">
        Hospital events <span className="text-slate-500">({events.data.length})</span>
      </h3>
      <ul className="divide-y divide-slate-800 text-sm">
        {events.data.map((e) => (
          <li key={e.id} className="grid grid-cols-12 items-center gap-2 py-2">
            <span className={`col-span-3 text-xs font-semibold uppercase ${kindTone(e.kind)}`}>
              {hospitalEventKindLabel(e.kind)}
            </span>
            <span className="col-span-6 text-slate-300">{e.detail ?? e.source}</span>
            <span className="col-span-3 text-right text-xs text-slate-500">
              {new Date(e.occurredAtUtc).toLocaleDateString()}
              {e.followedUp && <span className="ml-1 text-emerald-400">✓</span>}
            </span>
          </li>
        ))}
      </ul>
    </section>
  );
};
