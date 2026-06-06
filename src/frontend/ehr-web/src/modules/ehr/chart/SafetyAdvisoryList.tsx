import { advisoryCategoryLabel, type SafetyAdvisory } from "@/features/ehr/api/ehrApi";

/**
 * Renders point-of-care safety advisories returned by an order endpoint. Blocking advisories are
 * rose; non-blocking warnings are amber. Used by the Order Labs and Prescribe dialogs.
 */
export const SafetyAdvisoryList = ({ advisories }: { advisories: readonly SafetyAdvisory[] }) => (
  <ul className="space-y-1.5" aria-label="Safety advisories">
    {advisories.map((a, i) => (
      <li
        key={`${a.category}-${a.sourceRowId}-${i}`}
        className={`rounded-md border px-3 py-2 text-xs ${
          a.severity === "Blocking"
            ? "border-rose-600 bg-rose-950/50 text-rose-100"
            : "border-amber-600 bg-amber-950/40 text-amber-100"
        }`}
      >
        <span className="font-semibold uppercase tracking-wide">
          {advisoryCategoryLabel(a.category)}
        </span>{" "}
        — <span className="text-slate-100">{a.orderedConcept}</span> matches{" "}
        <span title={a.matchedCode}>{a.matchedDisplay}</span>{" "}
        <span className="text-slate-400">({a.sourceKind})</span>
        {a.detail && <span className="mt-0.5 block text-slate-300">{a.detail}</span>}
      </li>
    ))}
  </ul>
);
