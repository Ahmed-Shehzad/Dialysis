import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { MyOutsideRecordsCard } from "@/features/hie/components/MyOutsideRecordsCard";
import { usePatientPortalNotifications } from "@/features/notifications/usePatientPortalNotifications";
import { usePatientName } from "@/features/patients/usePatientName";
import { humanizeError } from "@/lib/api/humanizeError";
import { AfterVisitSummaryPanel } from "./AfterVisitSummaryPanel";
import { BookAppointmentDialog } from "./BookAppointmentDialog";
import { LabResultsPanel } from "./LabResultsPanel";
import { MessagesPanel } from "./MessagesPanel";
import { MyAppointmentRequestsPanel } from "./MyAppointmentRequestsPanel";
import { MyCarePlanPanel } from "./MyCarePlanPanel";
import { PatientConsentsPanel } from "./PatientConsentsPanel";
import { RecentTreatmentsPanel } from "./RecentTreatmentsPanel";
import { RemindersPanel } from "./RemindersPanel";
import { fetchAccessiblePatients, fetchPortalSummary, type PatientPortalSummary } from "./api";

const FALLBACK_DEMO_PATIENT_ID = "";

const claimAsString = (raw: unknown): string | null => {
  if (typeof raw === "string" && raw.trim().length > 0) return raw.trim();
  if (Array.isArray(raw) && typeof raw[0] === "string") return raw[0].trim();
  return null;
};

const Tile = ({
  label,
  value,
  tone,
  detail,
}: {
  label: string;
  value: number;
  tone: "clinic" | "amber" | "rose";
  detail: string;
}) => {
  const palette: Record<typeof tone, string> = {
    clinic: "border-clinic-700/60 bg-clinic-950/40 text-clinic-100",
    amber: "border-amber-700/60 bg-amber-950/30 text-amber-100",
    rose: "border-rose-700/60 bg-rose-950/40 text-rose-100",
  };
  return (
    <article className={`rounded-xl border p-4 ${palette[tone]}`}>
      <p className="text-xs uppercase tracking-wide opacity-80">{label}</p>
      <p className="mt-1 font-mono text-4xl font-semibold tabular-nums">{value}</p>
      <p className="mt-2 text-xs opacity-70">{detail}</p>
    </article>
  );
};

/**
 * Patient-facing portal landing page. Shows the signed-in patient's HIS counts —
 * upcoming appointments, open medication orders, open admissions — using the existing
 * `patient-access/portal-summary` endpoint.
 *
 * Patient identity is sourced from the auth context: a `his_patient_id` claim (or `sub`
 * fallback). When Keycloak isn't configured with a patient claim, the page falls back
 * to a manual id input so the demo loop is still exercisable. The HIS endpoint enforces
 * "patients see only their own counts" via the same claim, so the input is purely a
 * dev-mode affordance — in a real deployment the claim is the source of truth.
 */
export const PatientPortalPage = () => {
  const { user, status } = useAuth();

  // Patient identity is the `his_patient_id` claim only — `sub` is a *user* id, not a patient id, so it
  // must never be used to scope patient data. A real patient session carries `his_patient_id` and is
  // pinned to it; a staff/dev session (no such claim) falls through to the manual id box below.
  const claimPatientId = useMemo(() => claimAsString(user?.claims.his_patient_id), [user?.claims]);
  const [manualId, setManualId] = useState(FALLBACK_DEMO_PATIENT_ID);

  // No patient claim (staff/dev session) → discover patients that have portal data so the demo loop
  // can be opened from a dropdown instead of pasting a Guid.
  const accessiblePatients = useQuery({
    queryKey: ["patient-portal", "accessible-patients"],
    queryFn: () => fetchAccessiblePatients(),
    enabled: !claimPatientId,
    staleTime: 60_000,
  });

  const accessibleIds = accessiblePatients.data ?? [];

  // Default the selection to the first discovered patient once the list arrives (only if the user
  // hasn't already chosen / typed one).
  useEffect(() => {
    if (!claimPatientId && manualId.trim().length === 0 && accessibleIds.length > 0) {
      setManualId(accessibleIds[0]);
    }
  }, [claimPatientId, manualId, accessibleIds]);

  const patientId = claimPatientId ?? (manualId.trim().length > 0 ? manualId.trim() : null);

  const summary = useQuery({
    queryKey: ["patient-portal", "summary", patientId],
    queryFn: () => fetchPortalSummary(patientId as string),
    enabled: Boolean(patientId),
    staleTime: 30_000,
  });
  const { name: patientName } = usePatientName(patientId);
  const [bookOpen, setBookOpen] = useState(false);

  // Real-time care-team replies → toast + refetch.
  usePatientPortalNotifications(patientId);

  if (status === "loading") {
    return <div className="text-sm text-slate-400">Loading…</div>;
  }

  return (
    <div className="space-y-4">
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-xl font-semibold text-clinic-50">
            Your portal
            {patientName ? <span className="text-clinic-200"> · {patientName}</span> : null}
          </h2>
          <p className="text-sm text-slate-400">
            Appointments, medications, and admissions on file with the clinic.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setBookOpen(true)}
          disabled={!patientId}
          className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
        >
          + Book appointment
        </button>
      </header>

      {!claimPatientId && (
        <section className="space-y-2 rounded-lg border border-amber-700/60 bg-amber-950/30 p-3 text-sm text-amber-100">
          <p>
            No <span className="font-mono">his_patient_id</span> claim on your session — the IdP
            isn&apos;t configured with a patient-claim mapping yet. Pick a patient with data on file,
            or enter an id manually, for the demo loop:
          </p>
          {accessibleIds.length > 0 && (
            <select
              value={manualId}
              onChange={(e) => setManualId(e.target.value)}
              aria-label="Patient with portal data"
              className="w-full rounded-md border border-amber-700/70 bg-slate-950 px-3 py-1.5 font-mono text-xs text-slate-100 focus:border-amber-400 focus:outline-none"
            >
              {accessibleIds.map((id) => (
                <option key={id} value={id}>
                  {id}
                </option>
              ))}
            </select>
          )}
          <div className="flex items-center gap-2">
            <input
              type="text"
              value={manualId}
              onChange={(e) => setManualId(e.target.value)}
              placeholder="Patient Guid…"
              aria-label="Patient id"
              className="flex-1 rounded-md border border-amber-700/70 bg-slate-950 px-3 py-1.5 font-mono text-xs text-slate-100 focus:border-amber-400 focus:outline-none"
            />
          </div>
        </section>
      )}

      {!patientId && (
        <div className="rounded-md border border-dashed border-slate-700 p-3 text-sm text-slate-400">
          Enter a patient id above to load the portal summary.
        </div>
      )}

      {patientId && summary.isLoading && (
        <div className="text-sm text-slate-400">Loading your summary…</div>
      )}

      {patientId && summary.error && (
        <div
          role="alert"
          className="rounded-md border border-rose-700 bg-rose-900/40 p-3 text-sm text-rose-100"
        >
          {humanizeError(summary.error)}
        </div>
      )}

      {summary.data && <PortalTiles summary={summary.data} />}

      {patientId && <RemindersPanel patientId={patientId} />}
      {patientId && <RecentTreatmentsPanel patientId={patientId} />}
      {patientId && <AfterVisitSummaryPanel patientId={patientId} />}
      {patientId && <MyAppointmentRequestsPanel patientId={patientId} />}
      {patientId && <MessagesPanel patientId={patientId} />}
      {patientId && <MyCarePlanPanel patientId={patientId} />}
      {patientId && <LabResultsPanel patientId={patientId} />}
      {patientId && <MyOutsideRecordsCard patientId={patientId} />}
      {patientId && <PatientConsentsPanel patientId={patientId} />}

      {bookOpen && patientId && (
        <BookAppointmentDialog patientId={patientId} onClose={() => setBookOpen(false)} />
      )}
    </div>
  );
};

const PortalTiles = ({ summary }: { summary: PatientPortalSummary }) => (
  <section className="grid gap-3 sm:grid-cols-3" aria-label="Portal summary">
    <Tile
      label="Upcoming appointments"
      value={summary.upcomingAppointmentCount}
      tone="clinic"
      detail={
        summary.upcomingAppointmentCount === 0
          ? "Nothing on the calendar."
          : "Booked — the clinic will call to confirm if needed."
      }
    />
    <Tile
      label="Open medications"
      value={summary.openMedicationOrderCount}
      tone="amber"
      detail={
        summary.openMedicationOrderCount === 0
          ? "No active prescriptions on file."
          : "Active prescriptions tracked by HIS."
      }
    />
    <Tile
      label="Open admissions"
      value={summary.openAdmissionCount}
      tone={summary.openAdmissionCount > 0 ? "rose" : "clinic"}
      detail={summary.openAdmissionCount === 0 ? "Not currently admitted." : "Currently admitted."}
    />
  </section>
);
