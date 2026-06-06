import { useQuery } from "@tanstack/react-query";
import { fetchEmSuggestion } from "@/features/ehr/api/ehrApi";

/**
 * Coding-assist hint: suggests the E/M visit level the documented diagnoses support, so a visit isn't
 * under-coded. Advisory only — the biller decides. Renders nothing unless Ehr:Billing:EmCoding is
 * configured (the endpoint returns no suggestion otherwise).
 */
export const EmCodingHint = ({
  patientId,
  diagnosisCodes,
}: {
  patientId: string;
  diagnosisCodes: string[];
}) => {
  const suggestion = useQuery({
    queryKey: ["ehr", "em-suggestion", patientId, diagnosisCodes],
    queryFn: () => fetchEmSuggestion(diagnosisCodes),
    enabled: diagnosisCodes.length > 0,
  });

  if (!suggestion.data) return null;

  return (
    <section className="rounded-lg border border-emerald-700/60 bg-emerald-950/20 p-3">
      <p className="text-xs text-emerald-200">
        <span className="font-semibold">
          Suggested visit level: {suggestion.data.suggestedCptCode}
        </span>{" "}
        <span className="text-emerald-300/80">— {suggestion.data.rationale}</span>
      </p>
    </section>
  );
};
