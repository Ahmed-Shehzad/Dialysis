import { useQuery } from "@tanstack/react-query";
import {
  fetchSessionMedications,
  type MedicationEntry,
} from "@/features/medications/api/medicationsApi";

/**
 * Read-only Medications tab for the live-session view. Lists every MAR entry chronologically.
 * Recording a new administration / decline is delegated to RecordAdministrationDialog +
 * RecordDeclineDialog (PR 5 follow-up); this component covers the read surface so the
 * tab is useful even before the dialogs ship.
 */
type Props = { sessionId: string };

export const MedicationsTab = ({ sessionId }: Props) => {
  const query = useQuery({
    queryKey: ["pdms", "sessions", sessionId, "medications"],
    queryFn: () => fetchSessionMedications(sessionId),
    refetchInterval: 30_000,
  });

  if (query.isLoading) {
    return <div className="text-sm text-slate-400">Loading medications…</div>;
  }
  if (query.isError) {
    return <div className="text-sm text-rose-300">Could not load the MAR. Retry shortly.</div>;
  }

  const rows = query.data ?? [];
  if (rows.length === 0) {
    return (
      <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
        No medications recorded yet for this session.
      </div>
    );
  }

  return (
    <table className="w-full table-fixed border-collapse text-sm">
      <thead className="text-left text-slate-400">
        <tr>
          <th className="w-32 pb-2 font-medium">Time (UTC)</th>
          <th className="pb-2 font-medium">Medication</th>
          <th className="w-32 pb-2 font-medium">Dose</th>
          <th className="w-24 pb-2 font-medium">Route</th>
          <th className="pb-2 font-medium">Outcome</th>
        </tr>
      </thead>
      <tbody className="text-slate-200">
        {rows.map((row) => (
          <MedicationRow key={row.entryId} entry={row} />
        ))}
      </tbody>
    </table>
  );
};

const MedicationRow = ({ entry }: { entry: MedicationEntry }) => (
  <tr className="border-t border-slate-800/60">
    <td className="py-2 align-top font-mono text-xs text-slate-400">
      {new Date(entry.occurredAtUtc).toISOString().replace("T", " ").slice(0, 19)}
    </td>
    <td className="py-2 align-top">
      <div>{entry.medicationDisplay}</div>
      <div className="text-xs text-slate-500">
        {entry.medicationCodeSystem.split("/").pop()}:{entry.medicationCode}
      </div>
    </td>
    <td className="py-2 align-top">
      {entry.doseQuantity} {entry.doseUnit}
    </td>
    <td className="py-2 align-top">{entry.route}</td>
    <td className="py-2 align-top">
      {entry.wasAdministered ? (
        <span className="text-emerald-300">Administered</span>
      ) : (
        <span className="text-amber-300" title={entry.declineReason ?? undefined}>
          Declined{entry.declineReason ? `: ${entry.declineReason}` : ""}
        </span>
      )}
    </td>
  </tr>
);
