import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  clinicalNoteStatusLabel,
  DEMO_PROVIDER_ID,
  fetchPatientNotes,
  signClinicalNote,
  type ClinicalNoteListItem,
  type ClinicalNoteStatus,
} from "@/features/ehr/api/ehrApi";
import { humanizeError } from "@/lib/api/humanizeError";

const STATUS_TONE: Record<ClinicalNoteStatus, string> = {
  1: "border-amber-700/70 bg-amber-950/30 text-amber-100", // Draft
  2: "border-emerald-700/70 bg-emerald-950/40 text-emerald-100", // Signed
  3: "border-clinic-700/70 bg-clinic-950/40 text-clinic-100", // Amended
  4: "border-slate-700 bg-slate-900/40 text-slate-300", // EnteredInError
};

const formatDateTime = (iso: string): string => new Date(iso).toLocaleString();

/** Truncate at 140 chars so the row stays single-line at default zoom. */
const summary = (note: ClinicalNoteListItem): string => {
  const candidate = note.assessment || note.plan || note.subjective || note.objective;
  if (!candidate) return "—";
  const trimmed = candidate.trim();
  return trimmed.length > 140 ? `${trimmed.slice(0, 137)}…` : trimmed;
};

/** Aggregate requires a non-empty Assessment to sign; surface Sign only when satisfied. */
const canSign = (note: ClinicalNoteListItem): boolean =>
  note.status === 1 && note.assessment.trim().length > 0;

/**
 * Chart's Recent notes section. Lists the most recent clinical notes authored for the
 * patient across encounters, ordered most-recent first. Draft rows with a non-empty
 * assessment get a Sign action that promotes Draft → Signed through the existing
 * `POST /api/v1.0/clinical/notes/{id}/sign` endpoint (the aggregate's Sign method
 * enforces the assessment-required invariant).
 *
 * Status badge tone: amber Draft, emerald Signed, clinic Amended, slate EnteredInError.
 * Each row shows the short summary (assessment preferred, then plan/subjective/objective)
 * so a clinician can scan the chart without expanding every note.
 */
export const RecentNotesPanel = ({ patientId }: { patientId: string }) => {
  const queryClient = useQueryClient();

  const notes = useQuery({
    queryKey: ["ehr", "notes", patientId],
    queryFn: () => fetchPatientNotes(patientId),
    staleTime: 30_000,
  });

  const sign = useMutation({
    mutationFn: (noteId: string) =>
      signClinicalNote({ noteId, signingProviderId: DEMO_PROVIDER_ID }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["ehr", "notes", patientId] });
    },
  });

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <h3 className="mb-2 text-sm font-medium text-slate-200">
        Recent notes <span className="text-slate-500">({notes.data?.length ?? 0})</span>
      </h3>

      {notes.isLoading && <p className="text-xs text-slate-400">Loading notes…</p>}

      {notes.error && <p className="text-xs text-amber-300">{humanizeError(notes.error)}</p>}

      {notes.data && notes.data.length === 0 && (
        <p className="text-xs text-slate-500">
          No notes on file. Use <span className="font-medium">+ Add note</span> to record an
          observation.
        </p>
      )}

      {notes.data && notes.data.length > 0 && (
        <ul className="divide-y divide-slate-800 text-sm">
          {notes.data.map((n) => (
            <li key={n.id} className="grid grid-cols-12 items-start gap-2 py-2">
              <span className="col-span-3 text-xs text-slate-400">
                {formatDateTime(n.signedAtUtc ?? n.createdAtUtc)}
              </span>
              <span className="col-span-6 text-slate-200" title={n.assessment || undefined}>
                {summary(n)}
              </span>
              <span className="col-span-3 flex items-center justify-end gap-2">
                <span
                  className={`rounded-full border px-2 py-0.5 text-xs ${STATUS_TONE[n.status]}`}
                >
                  {clinicalNoteStatusLabel(n.status)}
                </span>
                {canSign(n) && (
                  <button
                    type="button"
                    onClick={() => sign.mutate(n.id)}
                    disabled={sign.isPending}
                    className="rounded-md border border-emerald-700/60 bg-emerald-950/30 px-2 py-0.5 text-xs text-emerald-200 transition hover:border-emerald-500 disabled:opacity-50"
                  >
                    Sign
                  </button>
                )}
              </span>
            </li>
          ))}
        </ul>
      )}

      {sign.error && (
        <p role="alert" className="mt-2 text-xs text-rose-300">
          {humanizeError(sign.error)}
        </p>
      )}
    </section>
  );
};
