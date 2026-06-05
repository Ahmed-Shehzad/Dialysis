import { Suspense, lazy } from "react";
import type { BokehChartProps } from "./BokehChartImpl";

export type { BokehChartProps, BokehSeries } from "./BokehChartImpl";

// echarts + echarts-for-react total ~1 MB once bundled. By making the chart implementation
// a lazy boundary the SessionLivePage shell (header, controls, latest-vitals panel) renders
// without waiting for it, and any page that doesn't actually render a chart never pays the
// cost. The wrapper keeps the public API identical so call sites don't change.
const Impl = lazy(() => import("./BokehChartImpl").then((m) => ({ default: m.BokehChartImpl })));

const ChartFallback = ({ height = 320 }: { height?: number }) => (
  <div
    role="status"
    style={{ height }}
    className="flex items-center justify-center rounded-md border border-slate-800 bg-slate-900/40 text-xs text-slate-500"
  >
    Loading chart…
  </div>
);

export const BokehChart = (props: BokehChartProps) => (
  <Suspense fallback={<ChartFallback height={props.height} />}>
    <Impl {...props} />
  </Suspense>
);
