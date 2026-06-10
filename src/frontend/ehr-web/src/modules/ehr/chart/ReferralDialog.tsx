import { useEffect, useId, useState } from "react";
import { createPortal } from "react-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { DEMO_PROVIDER_ID, REFERRAL_PARTNERS, requestReferral } from "@/features/ehr/api/ehrApi";
import { humanizeError } from "@/lib/api/humanizeError";

interface ReferralDialogProps {
  patientId: string;
  onClose(): void;
}

/**
 * Refers / transfers a patient to an external organisation. On submit the EHR raises
 * ReferralRequestedIntegrationEvent, which the HIE Outbound slice turns into a CCD pushed over
 * Directed Exchange. Referring provider is a demo constant until auth-claim → provider mapping lands.
 */
export const ReferralDialog = ({ patientId, onClose }: ReferralDialogProps) => {
  const titleId = useId();
  const queryClient = useQueryClient();
  const [partner, setPartner] = useState(REFERRAL_PARTNERS[0]?.id ?? "");
  const [reason, setReason] = useState("");

  const mutation = useMutation({
    mutationFn: () =>
      requestReferral({
        patientId,
        destinationPartnerId: partner,
        referringProviderId: DEMO_PROVIDER_ID,
        referralReason: reason.trim() || undefined,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["ehr", "referrals", patientId] });
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

  const canSubmit = !mutation.isPending && partner.length > 0;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    mutation.mutate();
  };

  const fieldClass =
    "mt-1 w-full rounded-md border border-slate-700 bg-slate-950 p-2 text-sm text-slate-100";

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
          <p className="text-xs uppercase tracking-wide text-slate-400">Refer / transfer</p>
          <h2 id={titleId} className="text-lg font-semibold text-clinic-50">
            Refer to an external organisation
          </h2>
          <p className="text-xs text-slate-400">
            A continuity-of-care document is assembled and pushed to the receiving organisation.
          </p>
        </header>

        <form onSubmit={handleSubmit} className="space-y-3">
          <fieldset className="space-y-3" disabled={mutation.isPending}>
            <legend className="sr-only">Referral</legend>
            <label className="block text-xs text-slate-300">
              Destination
              <select
                value={partner}
                onChange={(e) => setPartner(e.target.value)}
                className={fieldClass}
              >
                {REFERRAL_PARTNERS.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.display}
                  </option>
                ))}
              </select>
            </label>
            <label className="block text-xs text-slate-300">
              Reason (optional)
              <textarea
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                rows={3}
                className={fieldClass}
                placeholder="Clinical reason for the referral…"
              />
            </label>
          </fieldset>

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
              {mutation.isPending ? "Sending referral…" : "Send referral"}
            </button>
          </div>
        </form>
      </div>
    </div>,
    document.body,
  );
};
