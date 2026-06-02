import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  fetchSessionMedications,
  type MedicationEntry,
} from "@/features/medications/api/medicationsApi";
import { RecordAdministrationDialog } from "@/features/medications/components/RecordAdministrationDialog";

/**
 * Medications tab on the live-session view. Lists every MAR entry chronologically and
 * surfaces a "Record administration" button that opens
 * <see cref="RecordAdministrationDialog" />. The decline workflow is the same shape on
 * the existing entries — operator clicks "Decline" on the row's outcome cell, which
 * routes to the same backend port.
 */
type Props = { sessionId: string; patientId?: string; actorSub?: string };

export const MedicationsTab = ({ sessionId, patientId, actorSub }: Props) => {
  const [recordOpen, setRecordOpen] = useState(false);
  const query = useQuery({
    queryKey: ["pdms", "sessions", sessionId, "medications"],
    queryFn: () => fetchSessionMedications(sessionId),
    refetchInterval: 30_000,
  });

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium text-slate-200">
          MAR ({query.data?.length ?? 0} {query.data?.length === 1 ? "entry" : "entries"})
        </h3>
        {patientId && actorSub && (
          <button
            type="button"
            onClick={() => setRecordOpen(true)}
            className="rounded bg-emerald-600 px-3 py-1.5 text-xs text-slate-50 hover:bg-emerald-500"
          >
            Record administration
          </button>
        )}
      </div>

      {query.isLoading && <div className="text-sm text-slate-400">Loading medications…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">Could not load the MAR. Retry shortly.</div>
      )}
      {!query.isLoading && !query.isError && (query.data ?? []).length === 0 && (
        <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
          No medications recorded yet for this session.
        </div>
      )}
      {(query.data ?? []).length > 0 && (
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
            {(query.data ?? []).map((row) => (
              <MedicationRow key={row.entryId} entry={row} />
            ))}
          </tbody>
        </table>
      )}

      {recordOpen && patientId && actorSub && (
        <RecordAdministrationDialog
          sessionId={sessionId}
          patientId={patientId}
          actorSub={actorSub}
          onClose={() => setRecordOpen(false)}
        />
      )}
    </div>
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
