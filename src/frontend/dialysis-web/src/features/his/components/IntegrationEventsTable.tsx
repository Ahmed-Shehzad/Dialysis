import { useQuery } from "@tanstack/react-query";
import { fetchRecentIntegrationEvents } from "../api/hisApi";

const formatEventType = (qualified: string): string => {
  const sliceEnd = qualified.indexOf(",");
  const shortName = sliceEnd > 0 ? qualified.slice(0, sliceEnd) : qualified;
  const lastDot = shortName.lastIndexOf(".");
  return lastDot > 0 ? shortName.slice(lastDot + 1) : shortName;
};

export const IntegrationEventsTable = () => {
  const { data, isLoading, error } = useQuery({
    queryKey: ["his", "outbox-metadata"],
    queryFn: () => fetchRecentIntegrationEvents(25),
    refetchInterval: 15_000,
  });

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <h3 className="mb-3 text-sm font-medium text-slate-200">
        Cross-module integration events (HIS outbox)
      </h3>
      {isLoading && <div className="text-xs text-slate-400">Loading…</div>}
      {error && (
        <div className="text-xs text-rose-300">Failed to read the integration outbox.</div>
      )}
      {data && data.length === 0 && (
        <div className="text-xs text-slate-400">No events published recently.</div>
      )}
      {data && data.length > 0 && (
        <ul className="divide-y divide-slate-800 text-sm">
          {data.map((e) => (
            <li key={e.id} className="grid grid-cols-12 gap-3 py-2">
              <div className="col-span-5 font-mono text-xs text-slate-300">
                {formatEventType(e.assemblyQualifiedEventType)}
              </div>
              <div className="col-span-3 text-xs text-slate-400">
                {new Date(e.createdAtUtc).toLocaleString()}
              </div>
              <div className="col-span-2 text-xs text-slate-400">
                {e.processedAtUtc ? "delivered" : "pending"}
              </div>
              <div className="col-span-2 truncate font-mono text-xs text-slate-500" title={e.correlationId ?? ""}>
                {e.correlationId?.slice(0, 8) ?? ""}
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
};
