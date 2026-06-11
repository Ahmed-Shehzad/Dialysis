import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { fetchActiveSessions } from "@/features/sessions/api/sessionsApi";
import { useSessionReadings } from "@/features/sessions/hooks/useSessionReadings";
import { SessionLifecycleControls } from "@/features/sessions/components/SessionLifecycleControls";
import { TreatmentSummary } from "@/features/sessions/components/TreatmentSummary";
import { useVitalsStream } from "@/features/vitals/hooks/useVitalsStream";
import { useVitalsMonitorSound } from "@/features/vitals/hooks/useVitalsMonitorSound";
import { classifyVitalsSeverity } from "@/features/vitals/lib/vitalsSeverity";
import { VitalsChart } from "@/features/vitals/components/VitalsChart";
import { VitalsSoundToggle } from "@/features/vitals/components/VitalsSoundToggle";
import { MedicationsTab } from "@/features/medications/components/MedicationsTab";
import { SessionReportsTab } from "@/features/reports/components/SessionReportsTab";
import { ChairsideAlarmStrip } from "@/modules/pdms/chairside/ChairsideAlarmStrip";
import { ChairsideHeader } from "@/modules/pdms/chairside/ChairsideHeader";
import { KioskVitals } from "@/modules/pdms/chairside/KioskVitals";
import { LiveCostTile } from "@/modules/pdms/chairside/LiveCostTile";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { usePatientContext } from "@/shell/PatientContextProvider";
import { usePatientDemographics } from "@/features/patients/usePatientName";

type LiveTab = "vitals" | "medications" | "reports";

export const SessionLivePage = () => {
  const { sessionId } = useParams<{ sessionId: string }>();
  const history = useSessionReadings(sessionId);
  const stream = useVitalsStream(sessionId);

  // Lookup the session metadata so the lifecycle controls know the current status.
  const sessionsQuery = useQuery({
    queryKey: ["pdms", "sessions", "all-recent"],
    queryFn: () => fetchActiveSessions(false),
    enabled: Boolean(sessionId),
    refetchInterval: 15_000,
  });
  const session = sessionsQuery.data?.find((s) => s.id === sessionId);

  const merged = useMemo(() => {
    const seen = new Set<string>();
    const items = [...(history.data ?? []), ...stream.readings].filter((r) => {
      if (seen.has(r.readingId)) return false;
      seen.add(r.readingId);
      return true;
    });
    return items.sort(
      (a, b) => new Date(a.observedAtUtc).getTime() - new Date(b.observedAtUtc).getTime(),
    );
  }, [history.data, stream.readings]);

  const latest = merged.at(-1);

  // Chairside monitor sound: a steady beep per reading while vitals are moderate, switching to a
  // continuous critical alarm when the latest reading is in a life-threatening band. Only sounds
  // while the session is in progress; off until the clinician enables it (browser autoplay policy).
  const severity = latest ? classifyVitalsSeverity(latest) : "moderate";
  const sessionInProgress = session?.status === "InProgress";
  const monitorSound = useVitalsMonitorSound({
    severity,
    readingKey: latest?.readingId,
    active: sessionInProgress,
  });

  // Keep the cross-module patient context in sync with the session being watched, so a
  // nurse who opened the chairside monitor from the HIS queue (or anywhere else) sees the
  // same patient surfaced in the top-of-shell context bar and can jump back to their chart.
  const { patient, select } = usePatientContext();
  const { user } = useAuth();
  // EHR owns demographics — read them live so the context bar shows the real name/MRN (not a GUID).
  const { patient: ehrPatient, displayName: ehrName } = usePatientDemographics(session?.patientId);
  useEffect(() => {
    if (!session) return;
    const displayName = ehrName ?? `Patient ${session.patientId.slice(0, 8)}…`;
    const mrn = ehrPatient?.medicalRecordNumber;
    // Re-select on a patient change, or once the real name/MRN arrives to replace the placeholder.
    if (
      patient?.id === session.patientId &&
      patient.displayName === displayName &&
      patient.mrn === mrn
    ) {
      return;
    }
    select({ id: session.patientId, displayName, mrn });
  }, [session, patient, select, ehrName, ehrPatient]);

  const [tab, setTab] = useState<LiveTab>("vitals");

  if (!sessionId) {
    return <div className="text-slate-400">Missing session id.</div>;
  }

  return (
    <div className="space-y-6">
      <ChairsideHeader session={session} sessionId={sessionId} realtimeStatus={stream.status} />

      <ChairsideAlarmStrip />

      <SessionLifecycleControls session={session} />

      {sessionId && <TreatmentSummary sessionId={sessionId} />}

      <KioskVitals latest={latest} />

      {(session?.status === "InProgress" || stream.cost) && <LiveCostTile cost={stream.cost} />}

      <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <div className="mb-3 flex items-center gap-1 border-b border-slate-800/60">
          <TabButton active={tab === "vitals"} onClick={() => setTab("vitals")}>
            Vitals ({merged.length})
          </TabButton>
          <TabButton active={tab === "medications"} onClick={() => setTab("medications")}>
            Medications
          </TabButton>
          <TabButton active={tab === "reports"} onClick={() => setTab("reports")}>
            Documents
          </TabButton>
          {tab === "vitals" && (
            <div className="ml-auto pb-1">
              <VitalsSoundToggle
                enabled={monitorSound.enabled}
                severity={severity}
                active={sessionInProgress}
                onToggle={monitorSound.toggle}
              />
            </div>
          )}
        </div>
        {tab === "vitals" && <VitalsChart readings={merged} />}
        {tab === "medications" && (
          <MedicationsTab
            sessionId={sessionId}
            patientId={session?.patientId}
            actorSub={user?.username}
          />
        )}
        {tab === "reports" && (
          <SessionReportsTab sessionId={sessionId} patientId={session?.patientId} />
        )}
      </section>
    </div>
  );
};

const TabButton = ({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) => (
  <button
    type="button"
    onClick={onClick}
    className={
      "px-3 py-2 text-sm transition-colors " +
      (active
        ? "border-b-2 border-emerald-400 text-slate-100"
        : "text-slate-400 hover:text-slate-200")
    }
  >
    {children}
  </button>
);
