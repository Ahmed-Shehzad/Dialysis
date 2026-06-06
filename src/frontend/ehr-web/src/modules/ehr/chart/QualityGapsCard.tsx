import { useQuery } from "@tanstack/react-query";
import { fetchQualityGaps } from "@/features/ehr/api/ehrApi";

/**
 * Surfaces open quality / MIPS care gaps at the point of care so the provider documents the data the
 * measure needs. Renders nothing unless there are gaps (measures are config-driven and off by default).
 */
export const QualityGapsCard = ({ patientId }: { patientId: string }) => {
  const gaps = useQuery({
    queryKey: ["ehr", "quality-gaps", patientId],
    queryFn: () => fetchQualityGaps(patientId),
    enabled: Boolean(patientId),
  });

  if (!gaps.data || gaps.data.length === 0) return null;

  return (
    <section
      aria-label="Quality measure gaps"
      className="rounded-lg border border-amber-700 bg-amber-950/30 p-4"
    >
      <h3 className="mb-2 text-sm font-medium text-amber-200">
        Quality measures <span className="text-amber-400/80">({gaps.data.length} open)</span>
      </h3>
      <ul className="space-y-1.5">
        {gaps.data.map((g) => (
          <li key={g.measureId} className="rounded-md border border-amber-800/60 px-3 py-2 text-xs">
            <span className="font-semibold text-amber-100">{g.title}</span>
            <span className="ml-1 text-amber-200/80">— {g.detail}</span>
          </li>
        ))}
      </ul>
    </section>
  );
};
