import type { DurableCommandTrackingState } from "../hooks/useDurableCommand";

interface DurableCommandProgressProps {
  tracking: DurableCommandTrackingState;
  /** Optional human-readable label override; defaults to "Request". */
  label?: string;
}

/**
 * Small inline progress indicator that renders alongside a form using
 * <see cref="useDurableCommand"/>. Idle → empty; otherwise a pill showing the
 * current durability-pattern phase. Use as a visible affordance so users see
 * "queued → applied" even when the underlying enqueue returned 202 instantly.
 */
export const DurableCommandProgress = ({
  tracking,
  label = "Request",
}: DurableCommandProgressProps): JSX.Element | null => {
  if (tracking.phase === "idle") return null;

  const config = (() => {
    switch (tracking.phase) {
      case "enqueueing":
        return {
          text: `${label} sending…`,
          dot: "bg-slate-300 animate-pulse",
          chip: "border-slate-700 bg-slate-900/80 text-slate-200",
        };
      case "pending":
        return {
          text: `${label} queued — applying…`,
          dot: "bg-amber-300 animate-pulse",
          chip: "border-amber-700 bg-amber-950/60 text-amber-100",
        };
      case "applied":
        return {
          text: `${label} applied`,
          dot: "bg-emerald-400",
          chip: "border-emerald-700 bg-emerald-950/60 text-emerald-100",
        };
      case "failed":
        return {
          text: `${label} failed`,
          dot: "bg-rose-500",
          chip: "border-rose-700 bg-rose-950/60 text-rose-100",
        };
    }
  })();

  return (
    <span
      role="status"
      className={[
        "inline-flex items-center gap-2 rounded-full border px-3 py-1 text-xs font-medium",
        config.chip,
      ].join(" ")}
      data-testid="durable-command-progress"
      data-phase={tracking.phase}
    >
      <span className={["inline-block h-2 w-2 rounded-full", config.dot].join(" ")} aria-hidden />
      {config.text}
    </span>
  );
};
