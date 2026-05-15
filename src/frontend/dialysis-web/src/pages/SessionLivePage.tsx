import { useMemo } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { fetchActiveSessions } from "@/features/sessions/api/sessionsApi";
import { useSessionReadings } from "@/features/sessions/hooks/useSessionReadings";
import { SessionLifecycleControls } from "@/features/sessions/components/SessionLifecycleControls";
import { useVitalsStream } from "@/features/vitals/hooks/useVitalsStream";
import { VitalsChart } from "@/features/vitals/components/VitalsChart";
import { VitalsLatestPanel } from "@/features/vitals/components/VitalsLatestPanel";
import { StatusBadge } from "@/components/ui/StatusBadge";

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

  if (!sessionId) {
    return <div className="text-slate-400">Missing session id.</div>;
  }

  return (
    <div className="space-y-6">
      <header className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-clinic-50">Live session</h2>
          <p className="font-mono text-xs text-slate-400">{sessionId}</p>
        </div>
        <div className="flex items-center gap-2 text-xs text-slate-300">
          <span>Realtime</span>
          <StatusBadge status={stream.status} />
        </div>
      </header>

      <SessionLifecycleControls session={session} />

      <VitalsLatestPanel latest={latest} />

      <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <h3 className="mb-3 text-sm font-medium text-slate-200">
          Hemodynamics over time ({merged.length} samples)
        </h3>
        <VitalsChart readings={merged} />
      </section>
    </div>
  );
};
