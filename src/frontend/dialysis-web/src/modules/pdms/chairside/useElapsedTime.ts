import { useEffect, useState } from "react";

/** Optional pause-accounting inputs so the timer reflects machine on-time, not wall-clock. */
export type ElapsedTimeOptions = {
  /** Final end instant; when set the timer freezes at the start→end span. */
  endUtc?: string | null;
  /** Start of the current (open) pause; when set the timer freezes at that instant. */
  pausedAtUtc?: string | null;
  /** Seconds already spent paused (closed pauses), subtracted from elapsed. */
  pausedSeconds?: number | null;
};

/**
 * Returns the machine usage time for a session as a "HH:MM:SS" string — wall-clock since
 * `startUtc` minus all paused spans. While the session is actively running it ticks every
 * second; it freezes once the session ends (`endUtc`) or while it is paused (`pausedAtUtc`),
 * so the figure reflects actual machine on-time rather than wall-clock-since-start. Returns
 * "—" when no start time is set yet. The 1-Hz interval is cheap on the main thread; pausing
 * on tab blur is intentionally not done because clinical staff want the chairside monitor to
 * keep counting on a tablet that has lost focus.
 */
export const useElapsedTime = (
  startUtc: string | null | undefined,
  options: ElapsedTimeOptions = {},
): string => {
  const { endUtc, pausedAtUtc, pausedSeconds } = options;
  const [now, setNow] = useState<number>(() => Date.now());

  // Frozen whenever the session has ended or is currently paused — no need to tick.
  const frozen = Boolean(endUtc) || Boolean(pausedAtUtc);

  useEffect(() => {
    if (!startUtc || frozen) return;
    const id = globalThis.setInterval(() => setNow(Date.now()), 1000);
    return () => globalThis.clearInterval(id);
  }, [startUtc, frozen]);

  if (!startUtc) return "—";
  const startMs = new Date(startUtc).getTime();
  if (Number.isNaN(startMs)) return "—";

  // Reference instant: the end if ended, else the pause start if paused, else now.
  const referenceSource = endUtc ?? pausedAtUtc ?? null;
  const referenceMs = referenceSource ? new Date(referenceSource).getTime() : now;
  const reference = Number.isNaN(referenceMs) ? now : referenceMs;

  const grossSeconds = Math.floor((reference - startMs) / 1000);
  const totalSeconds = Math.max(0, grossSeconds - Math.max(0, pausedSeconds ?? 0));
  const h = Math.floor(totalSeconds / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  const s = totalSeconds % 60;
  const pad = (n: number) => n.toString().padStart(2, "0");
  return `${pad(h)}:${pad(m)}:${pad(s)}`;
};
