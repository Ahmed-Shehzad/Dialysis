type Status = "connected" | "connecting" | "reconnecting" | "disconnected" | "idle";

const styles: Record<Status, string> = {
  connected: "bg-emerald-500/20 text-emerald-300 ring-1 ring-emerald-400/50",
  connecting: "bg-sky-500/20 text-sky-300 ring-1 ring-sky-400/50",
  reconnecting: "bg-amber-500/20 text-amber-300 ring-1 ring-amber-400/50",
  disconnected: "bg-rose-500/20 text-rose-300 ring-1 ring-rose-400/50",
  idle: "bg-slate-500/20 text-slate-300 ring-1 ring-slate-400/50",
};

export const StatusBadge = ({ status }: { status: Status }) => (
  <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${styles[status]}`}>
    {status}
  </span>
);
