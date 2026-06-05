import { useEffect, useMemo, useState } from "react";
import { BokehChart, type BokehSeries } from "@/components/charts/BokehChart";
import type { VitalsReading } from "../api/vitalsApi";

type SeriesSpec = {
  key: keyof VitalsReading;
  label: string;
  color: string;
};

const SERIES: SeriesSpec[] = [
  { key: "systolicBloodPressure", label: "Systolic BP", color: "#ef4444" },
  { key: "diastolicBloodPressure", label: "Diastolic BP", color: "#3b82f6" },
  { key: "heartRateBpm", label: "Heart rate", color: "#f59e0b" },
];

const DEFAULT_WINDOW_SECONDS = 60;
const TICK_MS = 1_000;

export type VitalsChartProps = {
  readings: VitalsReading[];
  height?: number;
  /** Rolling visible window in seconds. New points enter on the right; older points slide off the left. Default 60s. */
  windowSeconds?: number;
};

/**
 * Bokeh-style interactive hemodynamics chart with a right-to-left sliding window.
 *
 * Single Responsibility: project `readings` into a BokehChart configuration; no fetching.
 * Holds one piece of local state — a 1Hz tick — so the x-axis max stays anchored to wall-clock
 * "now" even when no new readings arrive, which is what produces the horizontal scrolling.
 * `animationDurationUpdateMs` near the tick interval makes ECharts linearly interpolate the line
 * between updates so the motion looks continuous instead of stepping in discrete frames.
 */
export const VitalsChart = ({
  readings,
  height = 320,
  windowSeconds = DEFAULT_WINDOW_SECONDS,
}: VitalsChartProps) => {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const id = globalThis.setInterval(() => setNow(Date.now()), TICK_MS);
    return () => globalThis.clearInterval(id);
  }, []);

  const series = useMemo<BokehSeries[]>(() => {
    const sorted = [...readings].sort(
      (a, b) => new Date(a.observedAtUtc).getTime() - new Date(b.observedAtUtc).getTime(),
    );
    return SERIES.map((spec) => ({
      name: spec.label,
      color: spec.color,
      data: sorted.map((r) => [new Date(r.observedAtUtc), r[spec.key] as number]),
    }));
  }, [readings]);

  const windowMs = windowSeconds * 1_000;

  return (
    <BokehChart
      series={series}
      yAxisLabel="mmHg / bpm"
      xAxisType="time"
      height={height}
      emptyText="Awaiting readings…"
      xAxisMin={now - windowMs}
      xAxisMax={now}
      animationDurationUpdateMs={TICK_MS - 50}
    />
  );
};
