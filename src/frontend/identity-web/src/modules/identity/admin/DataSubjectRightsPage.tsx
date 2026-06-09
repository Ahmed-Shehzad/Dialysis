import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  exportPatientData,
  requestErasure,
  requestRestriction,
  type DataSubjectExport,
} from "@/features/data-protection/api/dataProtectionApi";
import {
  approveErasureRequest,
  fetchPendingErasureRequests,
  rejectErasureRequest,
  type ErasureRequestRow,
} from "@/features/data-protection/api/erasureApi";
import { PatientLabel } from "@/features/patients/PatientLabel";

/**
 * Operator-files-on-behalf-of-data-subject UI for GDPR Art. 15 (access), Art. 17
 * (erasure), and Art. 18 (restriction). Each tab runs against the same patient id
 * and surfaces the receipt the building-block service issued. The actual data
 * extraction (access) returns a FHIR-friendly bundle; erasure/restriction enqueue
 * an operator-review request the DPO triages.
 */
export const DataSubjectRightsPage = () => {
  const [patientId, setPatientId] = useState("");
  const [tab, setTab] = useState<"access" | "erasure" | "restriction" | "approvals">("access");

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-lg font-semibold text-slate-100">Data subject rights</h1>
        <p className="text-sm text-slate-400">
          File Art. 15 (access), Art. 17 (erasure), or Art. 18 (restriction) requests on behalf of a
          patient.
        </p>
      </div>

      <label className="flex items-center gap-2 text-sm">
        <span className="text-slate-400">Patient id</span>
        <input
          type="text"
          value={patientId}
          onChange={(e) => setPatientId(e.target.value)}
          placeholder="UUID"
          className="w-96 rounded border border-slate-700 bg-slate-800/60 px-2 py-1 font-mono text-slate-100"
        />
      </label>

      <div className="flex gap-1 border-b border-slate-800">
        {(["access", "erasure", "restriction", "approvals"] as const).map((t) => (
          <button
            key={t}
            type="button"
            onClick={() => setTab(t)}
            className={
              "px-3 py-1.5 text-sm capitalize " +
              (tab === t ? "border-b-2 border-emerald-400 text-slate-100" : "text-slate-400")
            }
          >
            {t}
          </button>
        ))}
      </div>

      {tab === "access" && <AccessTab patientId={patientId} />}
      {tab === "erasure" && (
        <RequestTab
          patientId={patientId}
          kind="erasure"
          submit={(id, by, reason) => requestErasure(id, by, reason)}
        />
      )}
      {tab === "restriction" && (
        <RequestTab
          patientId={patientId}
          kind="restriction"
          submit={(id, by, reason) => requestRestriction(id, by, reason)}
        />
      )}
      {tab === "approvals" && <ApprovalsTab />}
    </div>
  );
};

const ApprovalsTab = () => {
  const queryClient = useQueryClient();
  const [decidedBy, setDecidedBy] = useState("");

  const query = useQuery({
    queryKey: ["data-protection", "erasure", "pending"],
    queryFn: fetchPendingErasureRequests,
  });

  const invalidate = () =>
    queryClient.invalidateQueries({
      queryKey: ["data-protection", "erasure", "pending"],
      exact: false,
    });

  return (
    <div className="space-y-3">
      <p className="text-sm text-slate-300">
        Pending Art. 17 erasure requests. Approve runs every module's eraser in sequence (HIE
        Documents purges every <code>Current</code> document for the patient).
      </p>

      <label className="block text-sm">
        <span className="text-slate-400">Decision recorded as</span>
        <input
          type="text"
          value={decidedBy}
          onChange={(e) => setDecidedBy(e.target.value)}
          placeholder="DPO name"
          className="mt-1 w-96 rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
        />
      </label>

      {query.isLoading && <div className="text-sm text-slate-400">Loading pending requests…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">Could not load pending requests.</div>
      )}
      {!query.isLoading && (query.data?.length ?? 0) === 0 && (
        <div className="rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">
          No erasure requests pending.
        </div>
      )}

      <ul className="space-y-2">
        {(query.data ?? []).map((row) => (
          <ApprovalRow key={row.id} row={row} decidedBy={decidedBy} onDecision={invalidate} />
        ))}
      </ul>
    </div>
  );
};

const ApprovalRow = ({
  row,
  decidedBy,
  onDecision,
}: {
  row: ErasureRequestRow;
  decidedBy: string;
  onDecision: () => void;
}) => {
  const [reason, setReason] = useState("");

  const approve = useMutation({
    mutationFn: () => approveErasureRequest(row.id, decidedBy.trim()),
    onSuccess: onDecision,
  });
  const reject = useMutation({
    mutationFn: () => rejectErasureRequest(row.id, decidedBy.trim(), reason.trim()),
    onSuccess: onDecision,
  });

  const canDecide = decidedBy.trim().length > 0;

  return (
    <li className="rounded border border-slate-800 bg-slate-900/40 p-3 text-sm">
      <div className="flex items-center justify-between">
        <div>
          <span className="font-mono text-xs text-slate-400">{row.id}</span>
          <div className="text-slate-200">
            Patient <PatientLabel patientId={row.patientId} className="font-medium" />
          </div>
          <div className="text-xs text-slate-500">
            Requested by {row.requestedBy} ·{" "}
            {new Date(row.requestedAtUtc).toISOString().replace("T", " ").slice(0, 19)}
          </div>
          {row.reason && <div className="mt-1 text-xs italic text-slate-400">{row.reason}</div>}
        </div>
        <div className="flex flex-col gap-1">
          <button
            type="button"
            onClick={() => approve.mutate()}
            disabled={!canDecide || approve.isPending}
            className="rounded bg-emerald-600 px-3 py-1.5 text-xs text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
          >
            {approve.isPending ? "Approving…" : "Approve & execute"}
          </button>
        </div>
      </div>
      <div className="mt-2 flex items-center gap-2">
        <input
          type="text"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="Reject reason (legal hold, duplicate, …)"
          aria-label="Reject reason"
          className="flex-1 rounded border border-slate-700 bg-slate-800/60 px-2 py-1 text-xs text-slate-100"
        />
        <button
          type="button"
          onClick={() => reject.mutate()}
          disabled={!canDecide || reason.trim().length === 0 || reject.isPending}
          className="rounded border border-rose-700 px-3 py-1 text-xs text-rose-200 hover:border-rose-500 disabled:opacity-50"
        >
          {reject.isPending ? "Rejecting…" : "Reject"}
        </button>
      </div>
      {(approve.isError || reject.isError) && (
        <div className="mt-2 text-xs text-rose-300">Decision failed — retry shortly.</div>
      )}
    </li>
  );
};

const AccessTab = ({ patientId }: { patientId: string }) => {
  const mutation = useMutation<DataSubjectExport, Error>({
    mutationFn: () => exportPatientData(patientId.trim()),
  });

  const canSubmit = patientId.trim().length > 0 && !mutation.isPending;

  return (
    <div className="space-y-3">
      <p className="text-sm text-slate-300">
        Generates a patient-data export the subject can take with them (Art. 20 portability). Each
        module's <code>IModuleDataExtractor</code> contributes its slice of the bundle.
      </p>
      <button
        type="button"
        disabled={!canSubmit}
        onClick={() => mutation.mutate()}
        className="rounded bg-emerald-600 px-3 py-1.5 text-sm text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
      >
        {mutation.isPending ? "Exporting…" : "Run export"}
      </button>

      {mutation.isError && (
        <div className="text-sm text-rose-300">Export failed — verify the patient id.</div>
      )}

      {mutation.data && (
        <div className="space-y-2 rounded border border-slate-800 bg-slate-900/60 p-4 text-sm">
          <div className="text-xs text-slate-400">
            Generated{" "}
            {new Date(mutation.data.generatedAtUtc).toISOString().replace("T", " ").slice(0, 19)} Z
            — {mutation.data.resources.length} resources
          </div>
          <ul className="text-xs">
            {mutation.data.resources.map((r) => (
              <li
                key={`${r.resourceType}:${r.identifier}`}
                className="border-t border-slate-800/40 py-1"
              >
                <span className="font-mono text-slate-300">{r.resourceType}</span>{" "}
                <span className="text-slate-500">{r.identifier}</span>
              </li>
            ))}
          </ul>
          <a
            href={`data:application/json;charset=utf-8,${encodeURIComponent(
              JSON.stringify(mutation.data, null, 2),
            )}`}
            download={`patient-${patientId}-export.json`}
            className="inline-block rounded border border-slate-700 px-3 py-1.5 text-xs text-slate-200 hover:border-slate-500"
          >
            Download JSON
          </a>
        </div>
      )}
    </div>
  );
};

const RequestTab = ({
  patientId,
  kind,
  submit,
}: {
  patientId: string;
  kind: "erasure" | "restriction";
  submit: (id: string, by: string, reason: string) => Promise<{ requestId: string }>;
}) => {
  const [requestedBy, setRequestedBy] = useState("");
  const [reason, setReason] = useState("");

  const mutation = useMutation({
    mutationFn: () => submit(patientId.trim(), requestedBy.trim(), reason.trim()),
  });

  const canSubmit =
    patientId.trim().length > 0 &&
    requestedBy.trim().length > 0 &&
    reason.trim().length > 0 &&
    !mutation.isPending;

  return (
    <div className="space-y-3">
      <p className="text-sm text-slate-300">
        {kind === "erasure"
          ? "Files an Art. 17 erasure request. Clinical records may be exempt under legal-hold (30-year retention under Berufsordnung §10)."
          : "Restricts processing pending resolution of a dispute (Art. 18). Existing records stay readable but no new writes are allowed."}
      </p>

      <label className="block text-sm">
        <span className="text-slate-400">Requested by</span>
        <input
          type="text"
          value={requestedBy}
          onChange={(e) => setRequestedBy(e.target.value)}
          placeholder="DPO / operator name"
          className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
        />
      </label>

      <label className="block text-sm">
        <span className="text-slate-400">Reason</span>
        <textarea
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          rows={3}
          className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
          placeholder={
            kind === "erasure"
              ? "Patient requested deletion under Art. 17."
              : "Patient disputes accuracy of record under Art. 18."
          }
        />
      </label>

      <button
        type="button"
        onClick={() => mutation.mutate()}
        disabled={!canSubmit}
        className="rounded bg-emerald-600 px-3 py-1.5 text-sm text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
      >
        {mutation.isPending ? "Submitting…" : `File ${kind} request`}
      </button>

      {mutation.isError && (
        <div className="text-sm text-rose-300">Submission failed — retry shortly.</div>
      )}

      {mutation.data && (
        <div className="rounded border border-emerald-800/50 bg-emerald-900/20 p-3 text-sm text-emerald-200">
          Request filed. Tracking id:{" "}
          <span className="font-mono text-xs">{mutation.data.requestId}</span>
        </div>
      )}
    </div>
  );
};

export default DataSubjectRightsPage;
