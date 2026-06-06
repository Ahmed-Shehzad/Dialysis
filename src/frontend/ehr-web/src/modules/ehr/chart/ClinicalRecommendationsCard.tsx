import { useQuery } from "@tanstack/react-query";
import { type CdsRecommendation, fetchClinicalRecommendations } from "@/features/ehr/api/ehrApi";

/**
 * Point-of-care clinical decision support — condition-specific evidence-based prompts (e.g. asthma
 * controller med / spirometry, BP above target). Renders nothing unless rules are configured
 * (Ehr:Cds, off by default). Advisory only: the prompt names the action; ordering stays manual.
 */
export const ClinicalRecommendationsCard = ({ patientId }: { patientId: string }) => {
  const recs = useQuery({
    queryKey: ["ehr", "clinical-recommendations", patientId],
    queryFn: () => fetchClinicalRecommendations(patientId),
    enabled: Boolean(patientId),
  });

  if (!recs.data || recs.data.length === 0) return null;

  return (
    <section
      aria-label="Clinical recommendations"
      className="rounded-lg border border-sky-700 bg-sky-950/30 p-4"
    >
      <h3 className="mb-2 text-sm font-medium text-sky-200">
        Clinical decision support <span className="text-sky-400/80">({recs.data.length})</span>
      </h3>
      <ul className="space-y-1.5">
        {recs.data.map((r: CdsRecommendation) => (
          <li
            key={r.ruleId}
            className={`rounded-md border px-3 py-2 text-xs ${
              r.severity === "Warning" ? "border-amber-700/70 bg-amber-950/20" : "border-sky-800/60"
            }`}
          >
            <span className="font-semibold text-sky-100">{r.title}</span>
            {r.detail && <span className="ml-1 text-sky-200/80">— {r.detail}</span>}
            {r.suggestedActionKind && (
              <span className="ml-2 rounded-full bg-sky-900/70 px-2 py-0.5 text-[10px] uppercase tracking-wide text-sky-200">
                {r.suggestedActionKind}
                {r.suggestedActionCode ? ` ${r.suggestedActionCode}` : ""}
              </span>
            )}
          </li>
        ))}
      </ul>
    </section>
  );
};
