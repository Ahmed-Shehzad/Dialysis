import { useQuery } from "@tanstack/react-query";
import { fetchMyReminders, type PatientReminder } from "@/features/reminders/api/remindersApi";
import { humanizeError } from "@/lib/api/humanizeError";

/**
 * Plain-language health reminders for the patient — derived from the same quality-measure evaluator the
 * clinician chart uses, but phrased as concrete next steps. Empty unless quality measures are configured.
 */
export const RemindersPanel = ({ patientId }: { patientId: string }) => {
  const reminders = useQuery({
    queryKey: ["patient-portal", "reminders", patientId],
    queryFn: () => fetchMyReminders(patientId),
    staleTime: 60_000,
  });

  if (reminders.data && reminders.data.length === 0) return null;

  return (
    <section className="space-y-3 rounded-lg border border-amber-700/50 bg-amber-950/20 p-4">
      <header>
        <h3 className="text-sm font-medium text-amber-100">Things to do for your health</h3>
        <p className="text-xs text-amber-200/70">
          Suggested by your care team based on your record.
        </p>
      </header>

      {reminders.isLoading && <p className="text-xs text-amber-200/70">Loading…</p>}
      {reminders.error && <p className="text-xs text-rose-300">{humanizeError(reminders.error)}</p>}

      {reminders.data && reminders.data.length > 0 && (
        <ul className="space-y-2">
          {reminders.data.map((r: PatientReminder, i) => (
            <li key={i} className="rounded-md border border-amber-800/40 bg-amber-950/30 p-3">
              <p className="text-sm font-medium text-amber-50">{r.title}</p>
              <p className="text-xs text-amber-200/80">{r.whatToDo}</p>
              {r.resourceUrl && (
                <a
                  href={r.resourceUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="text-xs text-clinic-300 hover:underline"
                >
                  Learn more
                </a>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
};
