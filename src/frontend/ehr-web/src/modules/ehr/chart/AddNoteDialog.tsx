import { useEffect, useId, useState } from "react";
import { createPortal } from "react-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { DEMO_PROVIDER_ID, draftClinicalNote, startEncounter } from "@/features/ehr/api/ehrApi";
import { humanizeError } from "@/lib/api/humanizeError";

interface AddNoteDialogProps {
  patientId: string;
  onClose(): void;
}

interface AddNotePayload {
  patientId: string;
  body: string;
}

/**
 * Two-step server orchestration: a draft clinical note requires an Encounter, so the
 * dialog starts an ambulatory encounter for the patient on submit, then drafts the note
 * inside it. The "demo provider" (seeded by `EhrDemoSeeder` when `Ehr:Demo:Enabled=true`)
 * stands in for the authoring provider until real auth-claim → provider-id mapping lands.
 */
const submitNote = async ({ patientId, body }: AddNotePayload): Promise<string> => {
  const encounterId = await startEncounter({
    patientId,
    providerId: DEMO_PROVIDER_ID,
    encounterClassCode: "AMB",
  });
  return draftClinicalNote({
    encounterId,
    patientId,
    authoringProviderId: DEMO_PROVIDER_ID,
    // Map the free-text note into the SOAP "Subjective" field. The simplified
    // single-textarea UX is honest for nurse-style observations; full SOAP authoring
    // is a follow-up that exposes the other three sections.
    subjective: body,
    objective: "",
    assessment: "",
    plan: "",
  });
};

/**
 * Focused dialog for adding a clinical note to a patient chart. Captures a free-text
 * note body which is filed into the SOAP `Subjective` section of a freshly-created
 * ambulatory encounter. Closes on success; surfaces a humanised error otherwise.
 */
export const AddNoteDialog = ({ patientId, onClose }: AddNoteDialogProps) => {
  const titleId = useId();
  const queryClient = useQueryClient();
  const [body, setBody] = useState("");

  const mutation = useMutation({
    mutationFn: submitNote,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["ehr", "chart", patientId] });
      // RecentNotesPanel reads from this key — invalidate so the new note appears
      // immediately on the chart.
      void queryClient.invalidateQueries({ queryKey: ["ehr", "notes", patientId] });
      onClose();
    },
  });

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape" && !mutation.isPending) onClose();
    };
    globalThis.addEventListener("keydown", handler);
    return () => globalThis.removeEventListener("keydown", handler);
  }, [mutation.isPending, onClose]);

  const canSubmit = !mutation.isPending && body.trim().length > 0;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    mutation.mutate({ patientId, body: body.trim() });
  };

  return createPortal(
    <div
      role="presentation"
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      onClick={(e) => {
        // Backdrop-only dismissal — clicks inside the dialog bubble here with a
        // different target, so they never close it. Escape (wired above) is the
        // keyboard dismissal path.
        if (e.target === e.currentTarget && !mutation.isPending) onClose();
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="w-full max-w-lg rounded-lg border border-slate-800 bg-slate-900 p-6 shadow-xl"
      >
        <header className="mb-4">
          <p className="text-xs uppercase tracking-wide text-slate-400">Add note</p>
          <h2 id={titleId} className="text-lg font-semibold text-clinic-50">
            Clinical observation
          </h2>
          <p className="text-xs text-slate-400">
            Files into a fresh ambulatory encounter. Reviewed and signed later.
          </p>
        </header>

        <form onSubmit={handleSubmit} className="space-y-4">
          <label className="block text-sm">
            <span className="mb-1 block text-slate-300">Note</span>
            <textarea
              required
              autoFocus
              rows={6}
              value={body}
              onChange={(e) => setBody(e.target.value)}
              placeholder="Patient reports…"
              className="w-full resize-y rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100 focus:border-clinic-500 focus:outline-none"
            />
          </label>

          {mutation.error && (
            <p role="alert" className="text-sm text-rose-300">
              {humanizeError(mutation.error)}
            </p>
          )}

          <div className="flex items-center justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              disabled={mutation.isPending}
              className="rounded-md border border-slate-700 px-3 py-1.5 text-sm text-slate-300 transition hover:border-slate-500 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={!canSubmit}
              className="rounded-md bg-clinic-600 px-4 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {mutation.isPending ? "Saving…" : "Save note"}
            </button>
          </div>
        </form>
      </div>
    </div>,
    document.body,
  );
};
