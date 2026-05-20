import { useEffect, useMemo } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { fetchActiveSessions } from "@/features/sessions/api/sessionsApi";
import { useSessionReadings } from "@/features/sessions/hooks/useSessionReadings";
import { SessionLifecycleControls } from "@/features/sessions/components/SessionLifecycleControls";
import { TreatmentSummary } from "@/features/sessions/components/TreatmentSummary";
import { useVitalsStream } from "@/features/vitals/hooks/useVitalsStream";
import { VitalsChart } from "@/features/vitals/components/VitalsChart";
import { ChairsideHeader } from "@/modules/pdms/chairside/ChairsideHeader";
import { KioskVitals } from "@/modules/pdms/chairside/KioskVitals";
import { usePatientContext } from "@/shell/PatientContextProvider";

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

  // Keep the cross-module patient context in sync with the session being watched, so a
  // nurse who opened the chairside monitor from the HIS queue (or anywhere else) sees the
  // same patient surfaced in the top-of-shell context bar and can jump back to their chart.
  const { patient, select } = usePatientContext();
  useEffect(() => {
    if (!session) return;
    if (patient?.id === session.patientId) return;
    select({
      id: session.patientId,
      displayName: `Patient ${session.patientId.slice(0, 8)}…`,
    });
  }, [session, patient, select]);

  if (!sessionId) {
    return <div className="text-slate-400">Missing session id.</div>;
  }

  return (
    <div className="space-y-6">
      <ChairsideHeader session={session} sessionId={sessionId} realtimeStatus={stream.status} />

      <SessionLifecycleControls session={session} />

      {sessionId && <TreatmentSummary sessionId={sessionId} />}

      <KioskVitals latest={latest} />

      <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <h3 className="mb-3 text-sm font-medium text-slate-200">
          Hemodynamics over time ({merged.length} samples)
        </h3>
        <VitalsChart readings={merged} />
      </section>
    </div>
  );
};
