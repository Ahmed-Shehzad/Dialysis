import { useMemo } from "react";
import ReactECharts from "echarts-for-react";
import type { EChartsOption } from "echarts";

export type BokehSeries = {
  /** Display name shown in legend + tooltips. */
  name: string;
  /** [x, y] pairs. x can be a time-parseable string, Date, or number. */
  data: Array<[string | number | Date, number]>;
  /** Stroke colour for the line + markers. */
  color: string;
  /** Optional Y axis index (0 = left, 1 = right). Default 0. */
  yAxisIndex?: number;
  /** Show a filled area beneath the line. */
  area?: boolean;
  /** Disable markers (dots) on each datapoint. */
  hideSymbols?: boolean;
};

export type BokehChartProps = {
  series: BokehSeries[];
  /** Axis title for the left axis. */
  yAxisLabel?: string;
  /** Axis title for the optional right axis. */
  rightYAxisLabel?: string;
  /** Use 'time' for timestamps (default), 'value' for numeric, 'category' for discrete. */
  xAxisType?: "time" | "value" | "category";
  xAxisLabel?: string;
  height?: number;
  /** Empty-state placeholder. */
  emptyText?: string;
  /** Disable streaming-friendly animation. Default false (animation off). */
  animation?: boolean;
  /** Pin the visible x-axis range. Anchoring `xAxisMax` to "now" while keeping `xAxisMax - xAxisMin` constant produces a right-to-left sliding window. */
  xAxisMin?: number | Date;
  xAxisMax?: number | Date;
  /** Smooth-update easing between renders (ms). Pair with a linear easing while streaming to make the line slide rather than redraw discretely. Default 0 (no easing). */
  animationDurationUpdateMs?: number;
};

const DARK_GRID = "#1f2937";
const DARK_AXIS = "#475569";
const DARK_TEXT = "#cbd5e1";
const DARK_MUTED = "#64748b";

/**
 * Bokeh-style interactive chart wrapper around Apache ECharts.
 *
 * Bakes in the toolbar (pan, box-zoom, restore, save-PNG, box-select brush),
 * datazoom slider, crosshair tooltip, and dark-theme grid — the things that
 * make Bokeh feel Bokeh — so caller code only declares the series.
 *
 * Streaming-safe: animation defaults off so live appends don't flicker.
 */
export const BokehChart = ({
  series,
  yAxisLabel,
  rightYAxisLabel,
  xAxisType = "time",
  xAxisLabel,
  height = 320,
  emptyText = "Awaiting data…",
  animation = false,
  xAxisMin,
  xAxisMax,
  animationDurationUpdateMs = 0,
}: BokehChartProps) => {
  const option = useMemo<EChartsOption>(() => {
    const hasData = series.some((s) => s.data.length > 0);
    if (!hasData) {
      return {
        title: {
          text: emptyText,
          left: "center",
          top: "middle",
          textStyle: { color: DARK_MUTED, fontSize: 13, fontWeight: "normal" },
        },
      };
    }

    const yAxis: NonNullable<EChartsOption["yAxis"]> = [
      {
        type: "value",
        name: yAxisLabel,
        nameTextStyle: { color: DARK_TEXT, fontSize: 11 },
        axisLine: { lineStyle: { color: DARK_AXIS } },
        axisLabel: { color: DARK_TEXT, fontSize: 11 },
        splitLine: { lineStyle: { color: DARK_GRID } },
        scale: true,
      },
    ];
    if (rightYAxisLabel) {
      (yAxis as Array<unknown>).push({
        type: "value",
        name: rightYAxisLabel,
        nameTextStyle: { color: DARK_TEXT, fontSize: 11 },
        axisLine: { lineStyle: { color: DARK_AXIS } },
        axisLabel: { color: DARK_TEXT, fontSize: 11 },
        splitLine: { show: false },
        scale: true,
      });
    }

    const toMillis = (v: number | Date | undefined): number | undefined => {
      if (v === undefined) return undefined;
      return v instanceof Date ? v.getTime() : v;
    };
    const smoothUpdate = animationDurationUpdateMs > 0;
    return {
      animation: animation || smoothUpdate,
      animationDurationUpdate: smoothUpdate ? animationDurationUpdateMs : 0,
      animationEasingUpdate: smoothUpdate ? "linear" : "cubicOut",
      backgroundColor: "transparent",
      textStyle: { color: DARK_TEXT },
      legend: {
        data: series.map((s) => s.name),
        top: 6,
        textStyle: { color: DARK_TEXT, fontSize: 11 },
        icon: "roundRect",
        itemWidth: 14,
        itemHeight: 8,
      },
      grid: { left: 56, right: rightYAxisLabel ? 56 : 24, top: 36, bottom: 64 },
      tooltip: {
        trigger: "axis",
        axisPointer: { type: "cross", lineStyle: { color: DARK_AXIS } },
        backgroundColor: "rgba(15, 23, 42, 0.92)",
        borderColor: DARK_AXIS,
        textStyle: { color: DARK_TEXT, fontSize: 12 },
      },
      toolbox: {
        right: 12,
        iconStyle: { borderColor: DARK_TEXT },
        emphasis: { iconStyle: { borderColor: "#7dd3fc" } },
        feature: {
          dataZoom: { yAxisIndex: "none" },
          brush: { type: ["rect", "polygon", "clear"] },
          restore: {},
          saveAsImage: { backgroundColor: "#0f172a", pixelRatio: 2 },
        },
      },
      brush: {
        toolbox: ["rect", "polygon", "clear"],
        xAxisIndex: 0,
        throttleType: "debounce",
        throttleDelay: 100,
      },
      dataZoom: [
        { type: "inside", throttle: 50 },
        {
          type: "slider",
          height: 18,
          bottom: 16,
          borderColor: DARK_AXIS,
          backgroundColor: "rgba(15, 23, 42, 0.4)",
          fillerColor: "rgba(125, 211, 252, 0.18)",
          handleStyle: { color: "#7dd3fc" },
          textStyle: { color: DARK_TEXT, fontSize: 10 },
        },
      ],
      xAxis: {
        type: xAxisType,
        name: xAxisLabel,
        nameTextStyle: { color: DARK_TEXT, fontSize: 11 },
        axisLine: { lineStyle: { color: DARK_AXIS } },
        axisLabel: { color: DARK_TEXT, fontSize: 11, hideOverlap: true },
        splitLine: { show: false },
        min: toMillis(xAxisMin),
        max: toMillis(xAxisMax),
      },
      yAxis,
      series: series.map((s) => ({
        name: s.name,
        type: "line",
        showSymbol: !s.hideSymbols,
        symbolSize: 5,
        smooth: true,
        sampling: "lttb",
        yAxisIndex: s.yAxisIndex ?? 0,
        itemStyle: { color: s.color },
        lineStyle: { color: s.color, width: 2 },
        emphasis: { focus: "series" },
        areaStyle: s.area ? { color: s.color, opacity: 0.15 } : undefined,
        data: s.data.map(([x, y]) => [x instanceof Date ? x.getTime() : x, y]),
      })),
    };
  }, [
    series,
    yAxisLabel,
    rightYAxisLabel,
    xAxisType,
    xAxisLabel,
    emptyText,
    animation,
    xAxisMin,
    xAxisMax,
    animationDurationUpdateMs,
  ]);

  return (
    <ReactECharts
      option={option}
      notMerge
      lazyUpdate
      style={{ width: "100%", height }}
      theme="dark"
      opts={{ renderer: "canvas" }}
    />
  );
};
