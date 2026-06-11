import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import {
  scheduleSession,
  type ScheduleSessionRequest,
  type VascularAccessKind,
} from "../api/sessionsApi";
import { searchEhrPatients, type EhrPatient } from "@/features/ehr/api/ehrApi";

const ACCESS_KINDS: { value: VascularAccessKind; label: string }[] = [
  { value: "ArteriovenousFistula", label: "AV Fistula" },
  { value: "ArteriovenousGraft", label: "AV Graft" },
  { value: "CentralVenousCatheter", label: "CV Catheter" },
  { value: "PeritonealCatheter", label: "Peritoneal" },
];

const ANTICOAG_PROTOCOLS = ["HEPARIN_STD", "HEPARIN_LOW", "CITRATE", "NONE"];

const toLocalInputValue = (d: Date) => {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

const defaultStart = () => {
  const d = new Date();
  d.setMinutes(d.getMinutes() + 30);
  d.setSeconds(0, 0);
  return toLocalInputValue(d);
};

const defaultAccessDate = () => {
  const d = new Date();
  d.setMonth(d.getMonth() - 3);
  return d.toISOString().slice(0, 10);
};

export type ScheduleSessionDialogProps = {
  open: boolean;
  onClose: () => void;
  defaultPatientId?: string;
};

type FormState = Omit<ScheduleSessionRequest, "scheduledStartUtc">;

const initialForm = (defaultPatientId?: string): FormState => ({
  patientId: defaultPatientId ?? "",
  dialyzerModel: "FX-80",
  prescribedDurationMinutes: 240,
  bloodFlowRateMlPerMin: 350,
  dialysateFlowRateMlPerMin: 500,
  dialysatePotassiumMmolPerL: 2,
  dialysateCalciumMmolPerL: 1.25,
  dialysateSodiumMmolPerL: 138,
  targetUfVolumeLiters: 2.5,
  anticoagulationProtocolCode: "HEPARIN_STD",
  accessKind: "ArteriovenousFistula",
  accessSite: "Left forearm",
  accessEstablishedOn: defaultAccessDate(),
});

// Gate component: the dialog content only mounts while the dialog is open, so all form
// state initializes fresh on every open via plain useState initializers — replacing the
// old "reset state in an effect when `open` flips" pattern (react-hooks/set-state-in-effect).
export const ScheduleSessionDialog = ({
  open,
  onClose,
  defaultPatientId,
}: ScheduleSessionDialogProps) => {
  if (!open) return null;
  return <ScheduleSessionDialogContent onClose={onClose} defaultPatientId={defaultPatientId} />;
};

const ScheduleSessionDialogContent = ({
  onClose,
  defaultPatientId,
}: Omit<ScheduleSessionDialogProps, "open">) => {
  const queryClient = useQueryClient();
  const [form, setForm] = useState<FormState>(() => initialForm(defaultPatientId));
  const [localStart, setLocalStart] = useState(defaultStart());
  const [patientQuery, setPatientQuery] = useState("");
  const [selectedPatient, setSelectedPatient] = useState<EhrPatient | null>(null);

  const patientsQuery = useQuery({
    queryKey: ["ehr", "patients", "picker", patientQuery],
    queryFn: () => searchEhrPatients(patientQuery || undefined, 20),
    enabled: !selectedPatient,
    placeholderData: keepPreviousData,
  });

  const mutation = useMutation({
    mutationFn: () =>
      scheduleSession({
        ...form,
        scheduledStartUtc: new Date(localStart).toISOString(),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["pdms", "sessions"] });
      onClose();
    },
  });

  const canSubmit = useMemo(() => {
    return (
      form.patientId.length === 36 &&
      form.dialyzerModel.trim().length > 0 &&
      form.accessSite.trim().length > 0 &&
      !mutation.isPending
    );
  }, [form, mutation.isPending]);

  const update = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const pickPatient = (p: EhrPatient) => {
    setSelectedPatient(p);
    update("patientId", p.id);
  };
  const clearPatient = () => {
    setSelectedPatient(null);
    update("patientId", "");
    setPatientQuery("");
  };

  const labelClass = "block text-xs uppercase tracking-wide text-slate-400";
  const inputClass =
    "mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1.5 text-sm text-slate-100 focus:border-clinic-500 focus:outline-hidden";

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
      <div className="w-full max-w-3xl rounded-lg border border-slate-800 bg-slate-950 shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-800 px-5 py-3">
          <h3 className="text-base font-semibold text-clinic-50">Schedule dialysis session</h3>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="rounded-md px-2 py-1 text-xs text-slate-400 hover:bg-slate-800"
          >
            ✕
          </button>
        </div>

        <form
          className="grid max-h-[calc(100vh-12rem)] grid-cols-1 gap-4 overflow-y-auto p-5 md:grid-cols-3"
          onSubmit={(e) => {
            e.preventDefault();
            if (!canSubmit) return;
            mutation.mutate();
          }}
        >
          <div className="md:col-span-2">
            <span className={labelClass}>Patient</span>
            {selectedPatient ? (
              <div className="mt-1 flex items-center justify-between rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-sm">
                <div>
                  <div className="text-slate-100">
                    {selectedPatient.familyName}, {selectedPatient.givenName}
                  </div>
                  <div className="font-mono text-xs text-slate-400">
                    MRN {selectedPatient.medicalRecordNumber} · {selectedPatient.id.slice(0, 8)}
                  </div>
                </div>
                <button
                  type="button"
                  onClick={clearPatient}
                  className="rounded-md border border-slate-700 px-2 py-0.5 text-xs text-slate-300 hover:bg-slate-800"
                >
                  Change
                </button>
              </div>
            ) : (
              <div className="mt-1 space-y-1">
                <input
                  type="search"
                  value={patientQuery}
                  onChange={(e) => setPatientQuery(e.target.value)}
                  placeholder="Search by name or MRN…"
                  aria-label="Search patients"
                  className={inputClass}
                />
                <div className="max-h-44 overflow-y-auto rounded-md border border-slate-800 bg-slate-900/40">
                  {patientsQuery.isLoading && (
                    <div className="px-3 py-2 text-xs text-slate-400">Searching…</div>
                  )}
                  {patientsQuery.data?.length === 0 && (
                    <div className="px-3 py-2 text-xs text-slate-500">No patients matched.</div>
                  )}
                  {patientsQuery.data?.map((p) => (
                    <button
                      type="button"
                      key={p.id}
                      onClick={() => pickPatient(p)}
                      className="block w-full px-3 py-1.5 text-left text-sm text-slate-200 hover:bg-slate-800"
                    >
                      <span className="text-slate-100">
                        {p.familyName}, {p.givenName}
                      </span>{" "}
                      <span className="font-mono text-xs text-slate-400">
                        · MRN {p.medicalRecordNumber}
                      </span>
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>
          <div>
            <label htmlFor="schedule-start" className={labelClass}>
              Scheduled start (local)
            </label>
            <input
              id="schedule-start"
              required
              type="datetime-local"
              value={localStart}
              onChange={(e) => setLocalStart(e.target.value)}
              className={inputClass}
            />
          </div>

          <fieldset className="md:col-span-3 grid grid-cols-1 gap-4 rounded-md border border-slate-800 bg-slate-900/40 p-4 md:grid-cols-3">
            <legend className="px-2 text-xs uppercase tracking-wide text-slate-400">
              Prescription
            </legend>
            <div>
              <label htmlFor="dialyzer" className={labelClass}>
                Dialyzer model
              </label>
              <input
                id="dialyzer"
                required
                value={form.dialyzerModel}
                onChange={(e) => update("dialyzerModel", e.target.value)}
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="duration" className={labelClass}>
                Duration (min, 60–480)
              </label>
              <input
                id="duration"
                required
                type="number"
                min={60}
                max={480}
                value={form.prescribedDurationMinutes}
                onChange={(e) => update("prescribedDurationMinutes", Number(e.target.value))}
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="anticoag" className={labelClass}>
                Anticoagulation
              </label>
              <select
                id="anticoag"
                value={form.anticoagulationProtocolCode}
                onChange={(e) => update("anticoagulationProtocolCode", e.target.value)}
                className={inputClass}
              >
                {ANTICOAG_PROTOCOLS.map((p) => (
                  <option key={p} value={p}>
                    {p}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="qb" className={labelClass}>
                Blood flow mL/min (100–600)
              </label>
              <input
                id="qb"
                required
                type="number"
                min={100}
                max={600}
                value={form.bloodFlowRateMlPerMin}
                onChange={(e) => update("bloodFlowRateMlPerMin", Number(e.target.value))}
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="qd" className={labelClass}>
                Dialysate flow mL/min
              </label>
              <input
                id="qd"
                required
                type="number"
                value={form.dialysateFlowRateMlPerMin}
                onChange={(e) => update("dialysateFlowRateMlPerMin", Number(e.target.value))}
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="uf" className={labelClass}>
                Target UF L
              </label>
              <input
                id="uf"
                required
                type="number"
                step="0.1"
                min={0}
                value={form.targetUfVolumeLiters}
                onChange={(e) => update("targetUfVolumeLiters", Number(e.target.value))}
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="k" className={labelClass}>
                Dialysate K⁺ mmol/L
              </label>
              <input
                id="k"
                required
                type="number"
                step="0.1"
                value={form.dialysatePotassiumMmolPerL}
                onChange={(e) => update("dialysatePotassiumMmolPerL", Number(e.target.value))}
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="ca" className={labelClass}>
                Dialysate Ca²⁺ mmol/L
              </label>
              <input
                id="ca"
                required
                type="number"
                step="0.05"
                value={form.dialysateCalciumMmolPerL}
                onChange={(e) => update("dialysateCalciumMmolPerL", Number(e.target.value))}
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="na" className={labelClass}>
                Dialysate Na⁺ mmol/L
              </label>
              <input
                id="na"
                required
                type="number"
                value={form.dialysateSodiumMmolPerL}
                onChange={(e) => update("dialysateSodiumMmolPerL", Number(e.target.value))}
                className={inputClass}
              />
            </div>
          </fieldset>

          <fieldset className="md:col-span-3 grid grid-cols-1 gap-4 rounded-md border border-slate-800 bg-slate-900/40 p-4 md:grid-cols-3">
            <legend className="px-2 text-xs uppercase tracking-wide text-slate-400">
              Vascular access
            </legend>
            <div>
              <label htmlFor="access-kind" className={labelClass}>
                Kind
              </label>
              <select
                id="access-kind"
                value={form.accessKind}
                onChange={(e) => update("accessKind", e.target.value as VascularAccessKind)}
                className={inputClass}
              >
                {ACCESS_KINDS.map((k) => (
                  <option key={k.value} value={k.value}>
                    {k.label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="access-site" className={labelClass}>
                Site
              </label>
              <input
                id="access-site"
                required
                value={form.accessSite}
                onChange={(e) => update("accessSite", e.target.value)}
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="access-est" className={labelClass}>
                Established on
              </label>
              <input
                id="access-est"
                required
                type="date"
                value={form.accessEstablishedOn}
                onChange={(e) => update("accessEstablishedOn", e.target.value)}
                className={inputClass}
              />
            </div>
          </fieldset>

          {mutation.error && (
            <div className="md:col-span-3 rounded-md border border-rose-700 bg-rose-950/40 px-3 py-2 text-xs text-rose-200">
              Failed to schedule — server rejected the request. Check the prescription bounds.
            </div>
          )}

          <div className="md:col-span-3 flex justify-end gap-2 border-t border-slate-800 pt-3">
            <button
              type="button"
              onClick={onClose}
              className="rounded-md border border-slate-700 px-3 py-1.5 text-sm text-slate-300 hover:bg-slate-800"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={!canSubmit}
              className="rounded-md bg-clinic-600 px-4 py-1.5 text-sm font-medium text-white hover:bg-clinic-700 disabled:opacity-40"
            >
              {mutation.isPending ? "Scheduling…" : "Schedule"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
