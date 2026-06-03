import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import {
  exportPatientData,
  requestErasure,
  requestRestriction,
  type DataSubjectExport,
} from "@/features/data-protection/api/dataProtectionApi";

/**
 * Operator-files-on-behalf-of-data-subject UI for GDPR Art. 15 (access), Art. 17
 * (erasure), and Art. 18 (restriction). Each tab runs against the same patient id
 * and surfaces the receipt the building-block service issued. The actual data
 * extraction (access) returns a FHIR-friendly bundle; erasure/restriction enqueue
 * an operator-review request the DPO triages.
 */
export const DataSubjectRightsPage = () => {
  const [patientId, setPatientId] = useState("");
  const [tab, setTab] = useState<"access" | "erasure" | "restriction">("access");

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
        {(["access", "erasure", "restriction"] as const).map((t) => (
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
    </div>
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
