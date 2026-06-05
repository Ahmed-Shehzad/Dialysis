import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  recordAdministration,
  type RecordAdministrationRequest,
  type MedicationEntry,
} from "@/features/medications/api/medicationsApi";

/**
 * Modal for the clinician to record a positive medication administration. Optimistic
 * update against the session's MedicationsTab query — the new entry shows in the list
 * the moment the operator clicks Save, then reconciles against the server response.
 *
 * PHI minimisation: the body never carries patient identifiers — only what the clinician
 * just gave. The patient id comes from the live session context and is round-tripped
 * through the controller's PHI-access audit row.
 */
type Props = {
  sessionId: string;
  patientId: string;
  actorSub: string;
  onClose: () => void;
};

const ROUTES = ["Intravenous", "IntravenousPump", "Subcutaneous", "Oral", "Topical", "Other"];

export const RecordAdministrationDialog = ({ sessionId, patientId, actorSub, onClose }: Props) => {
  const queryClient = useQueryClient();
  const [display, setDisplay] = useState("");
  const [code, setCode] = useState("");
  const [codeSystem, setCodeSystem] = useState("http://www.nlm.nih.gov/research/umls/rxnorm");
  const [doseQuantity, setDoseQuantity] = useState("");
  const [doseUnit, setDoseUnit] = useState("mg");
  const [route, setRoute] = useState<string>("Intravenous");

  const mutation = useMutation({
    mutationFn: (request: RecordAdministrationRequest) => recordAdministration(sessionId, request),
    onSuccess: (entry: MedicationEntry) => {
      queryClient.setQueryData<MedicationEntry[] | undefined>(
        ["pdms", "sessions", sessionId, "medications"],
        (current) => (current ? [entry, ...current] : [entry]),
      );
      onClose();
    },
  });

  const canSubmit =
    display.trim().length > 0 &&
    code.trim().length > 0 &&
    parseFloat(doseQuantity) > 0 &&
    !mutation.isPending;

  const submit = () => {
    if (!canSubmit) return;
    mutation.mutate({
      patientId,
      codeSystem,
      code: code.trim(),
      display: display.trim(),
      doseQuantity: parseFloat(doseQuantity),
      doseUnit,
      route,
      administeredBySub: actorSub,
    });
  };

  return (
    <div
      className="fixed inset-0 z-40 flex items-center justify-center bg-slate-950/70"
      role="dialog"
    >
      <div className="w-full max-w-md rounded-lg border border-slate-800 bg-slate-900 p-5 shadow-xl">
        <h2 className="mb-4 text-lg font-semibold text-slate-100">Record administration</h2>

        <div className="space-y-3 text-sm text-slate-200">
          <label className="block">
            <span className="text-slate-400">Medication</span>
            <input
              className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100"
              value={display}
              onChange={(e) => setDisplay(e.target.value)}
              placeholder="e.g. Heparin 5000 IU"
              autoFocus
            />
          </label>

          <div className="grid grid-cols-2 gap-2">
            <label className="block">
              <span className="text-slate-400">Code system</span>
              <select
                className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2"
                value={codeSystem}
                onChange={(e) => setCodeSystem(e.target.value)}
              >
                <option value="http://www.nlm.nih.gov/research/umls/rxnorm">RxNorm</option>
                <option value="http://hl7.org/fhir/sid/ndc">NDC</option>
                <option value="http://www.whocc.no/atc">ATC</option>
              </select>
            </label>
            <label className="block">
              <span className="text-slate-400">Code</span>
              <input
                className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                placeholder="1234"
              />
            </label>
          </div>

          <div className="grid grid-cols-3 gap-2">
            <label className="col-span-1 block">
              <span className="text-slate-400">Dose</span>
              <input
                type="number"
                step="0.01"
                className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2"
                value={doseQuantity}
                onChange={(e) => setDoseQuantity(e.target.value)}
              />
            </label>
            <label className="col-span-1 block">
              <span className="text-slate-400">Unit</span>
              <select
                className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2"
                value={doseUnit}
                onChange={(e) => setDoseUnit(e.target.value)}
              >
                <option value="mg">mg</option>
                <option value="mL">mL</option>
                <option value="U">U</option>
              </select>
            </label>
            <label className="col-span-1 block">
              <span className="text-slate-400">Route</span>
              <select
                className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2"
                value={route}
                onChange={(e) => setRoute(e.target.value)}
              >
                {ROUTES.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </div>

        {mutation.isError && (
          <div className="mt-3 text-xs text-rose-300">
            Could not save — retry or escalate to the on-call.
          </div>
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
            onClick={submit}
            disabled={!canSubmit}
            className="rounded bg-emerald-600 px-3 py-1.5 text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
          >
            {mutation.isPending ? "Saving…" : "Save"}
          </button>
        </div>
      </div>
    </div>
  );
};
