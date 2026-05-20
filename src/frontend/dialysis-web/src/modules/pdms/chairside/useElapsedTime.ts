import { useEffect, useState } from "react";

/**
 * Returns the elapsed time since `startUtc` as a "HH:MM:SS" string, ticking every second.
 * Returns "—" when no start time is set yet (session scheduled but not started). The 1-Hz
 * interval is cheap on the main thread; pausing on tab blur is intentionally not done
 * because clinical staff want the chairside monitor to keep counting on a tablet that has
 * lost focus.
 */
export const useElapsedTime = (startUtc: string | null | undefined): string => {
  const [now, setNow] = useState<number>(() => Date.now());

  useEffect(() => {
    if (!startUtc) return;
    const id = globalThis.setInterval(() => setNow(Date.now()), 1000);
    return () => globalThis.clearInterval(id);
  }, [startUtc]);

  if (!startUtc) return "—";
  const startMs = new Date(startUtc).getTime();
  if (Number.isNaN(startMs)) return "—";
  const totalSeconds = Math.max(0, Math.floor((now - startMs) / 1000));
  const h = Math.floor(totalSeconds / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  const s = totalSeconds % 60;
  const pad = (n: number) => n.toString().padStart(2, "0");
  return `${pad(h)}:${pad(m)}:${pad(s)}`;
};
