import type { SessionCost } from "@/features/vitals/api/vitalsApi";

type Props = {
  cost: SessionCost | null;
};

const money = (amount: number, currency: string): string => {
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
};

/**
 * Chairside-scale live billing tile. Shows the running, itemised treatment cost streamed from
 * PDMS over the vitals hub while the session is in progress. The figure is an estimate that
 * converges to the invoice EHR captures at completion (both use the same tariff).
 */
export const LiveCostTile = ({ cost }: Props) => (
  <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
    <div className="mb-3 flex items-baseline justify-between">
      <h3 className="text-sm font-medium text-slate-200">Live treatment cost (estimate)</h3>
      {cost && (
        <span className="text-xs text-slate-500">
          {cost.elapsedMinutes} min · updated {new Date(cost.asOfUtc).toLocaleTimeString()}
        </span>
      )}
    </div>

    {!cost ? (
      <p className="text-sm text-slate-500">Waiting for the first cost tick…</p>
    ) : (
      <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div>
          <div className="text-4xl font-semibold tabular-nums text-emerald-300">
            {money(cost.total, cost.currencyCode)}
          </div>
          <div className="text-xs text-slate-500">accrued so far</div>
        </div>
        <ul className="flex-1 space-y-1 text-xs text-slate-300 md:max-w-sm">
          {cost.lines.map((line) => (
            <li key={line.label} className="flex items-center justify-between gap-3">
              <span className="truncate text-slate-400">{line.label}</span>
              <span className="shrink-0 tabular-nums">{money(line.amount, cost.currencyCode)}</span>
            </li>
          ))}
        </ul>
      </div>
    )}
  </section>
);
