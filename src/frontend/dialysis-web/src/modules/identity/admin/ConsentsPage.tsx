import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  fetchConsentsForPatient,
  grantConsent,
  revokeConsent,
  type Consent,
} from "@/features/data-protection/api/dataProtectionApi";

/**
 * Per-patient cross-organization consent management. Calls the HIE
 * <c>ConsentAdminController</c> to list, grant, and revoke consents that gate inbound
 * and outbound FHIR resources to/from a partner organization.
 *
 * The operator types the patient id (UUID v7), the partner id, the scope (e.g.
 * <c>Patient.read</c>, <c>Observation.read</c>) and the direction, then saves. The
 * list reloads automatically on save.
 */
export const ConsentsPage = () => {
  const queryClient = useQueryClient();
  const [patientId, setPatientId] = useState("");
  const [submittedPatientId, setSubmittedPatientId] = useState<string | null>(null);
  const [showGrantDrawer, setShowGrantDrawer] = useState(false);

  const query = useQuery({
    queryKey: ["identity", "consents", submittedPatientId],
    queryFn: () => fetchConsentsForPatient(submittedPatientId!),
    enabled: Boolean(submittedPatientId),
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["identity", "consents"], exact: false });

  const revokeMutation = useMutation({
    mutationFn: (consentId: string) => revokeConsent(consentId),
    onSuccess: invalidate,
  });

  const rows: Consent[] = query.data ?? [];

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-lg font-semibold text-slate-100">Patient consents</h1>
        <p className="text-sm text-slate-400">
          Cross-organization consent gating — what a partner is allowed to exchange for this
          patient.
        </p>
      </div>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (patientId.trim().length > 0) setSubmittedPatientId(patientId.trim());
        }}
        className="flex items-center gap-2"
      >
        <input
          type="text"
          value={patientId}
          onChange={(e) => setPatientId(e.target.value)}
          placeholder="Patient id (UUID)"
          className="w-96 rounded border border-slate-700 bg-slate-800/60 px-2 py-1 text-sm font-mono text-slate-100"
        />
        <button
          type="submit"
          className="rounded border border-slate-700 px-3 py-1 text-sm text-slate-200 hover:border-slate-500"
        >
          Load
        </button>
        {submittedPatientId && (
          <button
            type="button"
            onClick={() => setShowGrantDrawer(true)}
            className="rounded bg-emerald-600 px-3 py-1 text-sm text-slate-50 hover:bg-emerald-500"
          >
            Grant new
          </button>
        )}
      </form>

      {query.isLoading && <div className="text-sm text-slate-400">Loading consents…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">Could not load consents. Verify the patient id.</div>
      )}

      {submittedPatientId && !query.isLoading && rows.length === 0 && (
        <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
          No consents on record. The patient hasn't authorised any partner yet.
        </div>
      )}

      {rows.length > 0 && (
        <table className="w-full table-fixed border-collapse text-sm">
          <thead className="text-left text-slate-400">
            <tr>
              <th className="w-40 pb-2 font-medium">Consent id</th>
              <th className="w-40 pb-2 font-medium">Partner</th>
              <th className="pb-2 font-medium">Scope</th>
              <th className="w-24 pb-2 font-medium">Direction</th>
              <th className="pb-2 font-medium">Effective</th>
              <th className="w-24 pb-2 font-medium">Status</th>
              <th className="w-24 pb-2 font-medium">Actions</th>
            </tr>
          </thead>
          <tbody className="text-slate-200">
            {rows.map((row) => (
              <tr key={row.consentId} className="border-t border-slate-800/60">
                <td className="py-2 align-top font-mono text-xs">{row.consentId.slice(0, 8)}…</td>
                <td className="py-2 align-top font-mono text-xs">{row.partnerId}</td>
                <td className="py-2 align-top">{row.scope}</td>
                <td className="py-2 align-top text-xs">
                  {row.direction === 0 ? "Inbound" : "Outbound"}
                </td>
                <td className="py-2 align-top text-xs text-slate-400">
                  {new Date(row.effectiveFromUtc).toISOString().slice(0, 10)} →{" "}
                  {row.effectiveToUtc
                    ? new Date(row.effectiveToUtc).toISOString().slice(0, 10)
                    : "open"}
                </td>
                <td className="py-2 align-top">{row.status}</td>
                <td className="py-2 align-top">
                  {row.status === "Granted" && (
                    <button
                      type="button"
                      onClick={() => revokeMutation.mutate(row.consentId)}
                      disabled={revokeMutation.isPending}
                      className="rounded border border-rose-700/50 px-2 py-1 text-xs text-rose-200 hover:border-rose-500 disabled:opacity-50"
                    >
                      Revoke
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {showGrantDrawer && submittedPatientId && (
        <GrantConsentDrawer
          patientId={submittedPatientId}
          onClose={() => setShowGrantDrawer(false)}
          onGranted={invalidate}
        />
      )}
    </div>
  );
};

const GrantConsentDrawer = ({
  patientId,
  onClose,
  onGranted,
}: {
  patientId: string;
  onClose: () => void;
  onGranted: () => void;
}) => {
  const [partnerId, setPartnerId] = useState("");
  const [scope, setScope] = useState("Patient.read");
  const [direction, setDirection] = useState(0);
  const [effectiveFromUtc, setEffectiveFromUtc] = useState(new Date().toISOString().slice(0, 16));
  const [effectiveToUtc, setEffectiveToUtc] = useState("");

  const mutation = useMutation({
    mutationFn: () =>
      grantConsent({
        patientId,
        partnerId: partnerId.trim(),
        scope: scope.trim(),
        direction,
        effectiveFromUtc: new Date(effectiveFromUtc).toISOString(),
        effectiveToUtc: effectiveToUtc ? new Date(effectiveToUtc).toISOString() : null,
      }),
    onSuccess: () => {
      onGranted();
      onClose();
    },
  });

  const canSubmit = partnerId.trim().length > 0 && scope.trim().length > 0 && !mutation.isPending;

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-end bg-slate-950/70" role="dialog">
      <div className="h-full w-full max-w-md border-l border-slate-800 bg-slate-900 p-5 shadow-xl">
        <h2 className="mb-1 text-lg font-semibold text-slate-100">Grant consent</h2>
        <div className="mb-4 text-xs text-slate-500 font-mono">{patientId}</div>

        <label className="block text-sm">
          <span className="text-slate-400">Partner id</span>
          <input
            type="text"
            value={partnerId}
            onChange={(e) => setPartnerId(e.target.value)}
            className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100 font-mono"
          />
        </label>

        <label className="mt-3 block text-sm">
          <span className="text-slate-400">Scope</span>
          <input
            type="text"
            value={scope}
            onChange={(e) => setScope(e.target.value)}
            placeholder="Patient.read"
            className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
          />
        </label>

        <label className="mt-3 block text-sm">
          <span className="text-slate-400">Direction</span>
          <select
            value={direction}
            onChange={(e) => setDirection(parseInt(e.target.value, 10))}
            className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
          >
            <option value={0}>Inbound (partner → platform)</option>
            <option value={1}>Outbound (platform → partner)</option>
          </select>
        </label>

        <div className="mt-3 grid grid-cols-2 gap-3">
          <label className="block text-sm">
            <span className="text-slate-400">Effective from</span>
            <input
              type="datetime-local"
              value={effectiveFromUtc}
              onChange={(e) => setEffectiveFromUtc(e.target.value)}
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
            />
          </label>
          <label className="block text-sm">
            <span className="text-slate-400">Effective until (optional)</span>
            <input
              type="datetime-local"
              value={effectiveToUtc}
              onChange={(e) => setEffectiveToUtc(e.target.value)}
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
            />
          </label>
        </div>

        {mutation.isError && (
          <div className="mt-3 text-xs text-rose-300">Save failed — retry shortly.</div>
        )}

        <div className="mt-5 flex justify-end gap-2 text-sm">
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-700 px-3 py-1.5 text-slate-200 hover:border-slate-500"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={() => mutation.mutate()}
            disabled={!canSubmit}
            className="rounded bg-emerald-600 px-3 py-1.5 text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
          >
            {mutation.isPending ? "Saving…" : "Grant"}
          </button>
        </div>
      </div>
    </div>
  );
};

export default ConsentsPage;
