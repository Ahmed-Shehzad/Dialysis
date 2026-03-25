import { useCallback, useId, useState } from "react";
import type { SubmitEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { recordPreAssessment } from "../../api";
import type { PreAssessmentContext } from "../../types";

const ACCESS_TYPES = ["AVF", "AVG", "CVC"] as const;

interface PreAssessmentPanelProps {
    sessionId?: string | null;
    patientMrn?: string | null;
    preAssessment?: PreAssessmentContext | null;
    onSessionSelect?: (sessionId: string | null) => void;
}

export function PreAssessmentPanel(props: Readonly<PreAssessmentPanelProps>) {
    const formInstanceKey = `${props.sessionId ?? "no-session"}-${props.preAssessment?.recordedAt ?? "new"}`;
    return <PreAssessmentPanelInner key={formInstanceKey} {...props} />;
}

function PreAssessmentPanelInner({
    sessionId,
    preAssessment,
}: Readonly<PreAssessmentPanelProps>) {
    const queryClient = useQueryClient();
    const formFieldIdsPrefix = useId();
    const preWeightFieldId = `${formFieldIdsPrefix}-pre-weight`;
    const bpSystolicFieldId = `${formFieldIdsPrefix}-bp-systolic`;
    const bpDiastolicFieldId = `${formFieldIdsPrefix}-bp-diastolic`;
    const accessTypeFieldId = `${formFieldIdsPrefix}-access-type`;
    const painNotesFieldId = `${formFieldIdsPrefix}-pain-notes`;
    const [preWeightKg, setPreWeightKg] = useState<string>(preAssessment?.preWeightKg?.toString() ?? "");
    const [bpSystolic, setBpSystolic] = useState<string>(preAssessment?.bpSystolic?.toString() ?? "");
    const [bpDiastolic, setBpDiastolic] = useState<string>(preAssessment?.bpDiastolic?.toString() ?? "");
    const [accessType, setAccessType] = useState<string>(preAssessment?.accessTypeValue ?? "");
    const [prescriptionConfirmed, setPrescriptionConfirmed] = useState<boolean>(preAssessment?.prescriptionConfirmed ?? false);
    const [painNotes, setPainNotes] = useState<string>(preAssessment?.painSymptomNotes ?? "");

    const recordMutation = useMutation({
        mutationFn: () =>
            recordPreAssessment(sessionId!, {
                preWeightKg: preWeightKg ? Number(preWeightKg) : undefined,
                bpSystolic: bpSystolic ? Number(bpSystolic) : undefined,
                bpDiastolic: bpDiastolic ? Number(bpDiastolic) : undefined,
                accessTypeValue: accessType || undefined,
                prescriptionConfirmed,
                painSymptomNotes: painNotes || undefined,
            }),
        onSuccess: () => {
            void queryClient.invalidateQueries({ queryKey: ["treatment-session", sessionId] });
        },
    });

    const handleSubmit = useCallback(
        (e: SubmitEvent<HTMLFormElement>) => {
            e.preventDefault();
            if (!sessionId) return;
            recordMutation.mutate();
        },
        [sessionId, recordMutation]
    );

    const isComplete = prescriptionConfirmed && (preWeightKg !== "" || bpSystolic !== "" || accessType !== "");

    return (
        <div className="rounded-lg border border-amber-200 bg-amber-50/50 p-6">
            <div className="mb-4 flex items-center gap-2">
                <span className="rounded bg-amber-500 px-2 py-0.5 text-xs font-medium text-white">
                    Pre-Assessment
                </span>
            </div>
            <form onSubmit={handleSubmit} className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
                <div>
                    <label htmlFor={preWeightFieldId} className="block text-xs font-medium text-amber-800">
                        Pre-weight (kg)
                    </label>
                    <input
                        id={preWeightFieldId}
                        type="number"
                        step="0.1"
                        value={preWeightKg}
                        onChange={(e) => setPreWeightKg(e.target.value)}
                        className="mt-0.5 w-full rounded border border-amber-300 bg-white px-2 py-1.5 text-sm"
                        placeholder="—"
                    />
                </div>
                <div>
                    <label htmlFor={bpSystolicFieldId} className="block text-xs font-medium text-amber-800">
                        BP systolic (mmHg)
                    </label>
                    <input
                        id={bpSystolicFieldId}
                        type="number"
                        value={bpSystolic}
                        onChange={(e) => setBpSystolic(e.target.value)}
                        className="mt-0.5 w-full rounded border border-amber-300 bg-white px-2 py-1.5 text-sm"
                        placeholder="—"
                    />
                </div>
                <div>
                    <label htmlFor={bpDiastolicFieldId} className="block text-xs font-medium text-amber-800">
                        BP diastolic (mmHg)
                    </label>
                    <input
                        id={bpDiastolicFieldId}
                        type="number"
                        value={bpDiastolic}
                        onChange={(e) => setBpDiastolic(e.target.value)}
                        className="mt-0.5 w-full rounded border border-amber-300 bg-white px-2 py-1.5 text-sm"
                        placeholder="—"
                    />
                </div>
                <div>
                    <label htmlFor={accessTypeFieldId} className="block text-xs font-medium text-amber-800">
                        Access type
                    </label>
                    <select
                        id={accessTypeFieldId}
                        value={accessType}
                        onChange={(e) => setAccessType(e.target.value)}
                        className="mt-0.5 w-full rounded border border-amber-300 bg-white px-2 py-1.5 text-sm"
                    >
                        <option value="">—</option>
                        {ACCESS_TYPES.map((t) => (
                            <option key={t} value={t}>
                                {t}
                            </option>
                        ))}
                    </select>
                </div>
                <div>
                    <label htmlFor={painNotesFieldId} className="block text-xs font-medium text-amber-800">
                        Pain / symptom check
                    </label>
                    <input
                        id={painNotesFieldId}
                        type="text"
                        value={painNotes}
                        onChange={(e) => setPainNotes(e.target.value)}
                        className="mt-0.5 w-full rounded border border-amber-300 bg-white px-2 py-1.5 text-sm"
                        placeholder="—"
                    />
                </div>
                <div className="flex items-end">
                    <label className="flex items-center gap-2">
                        <input
                            type="checkbox"
                            checked={prescriptionConfirmed}
                            onChange={(e) => setPrescriptionConfirmed(e.target.checked)}
                            className="rounded border-amber-300"
                        />
                        <span className="text-xs font-medium text-amber-800">Prescription confirmed</span>
                    </label>
                </div>
            </form>
            <div className="mt-6 flex flex-wrap gap-3">
                {sessionId && (
                    <>
                        {recordMutation.error && (
                            <p className="w-full text-sm text-red-600">{recordMutation.error.message}</p>
                        )}
                        <button
                            type="submit"
                            disabled={!isComplete || recordMutation.isPending}
                            className="rounded bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                            {recordMutation.isPending ? "Saving…" : "Save Pre-Assessment"}
                        </button>
                        {preAssessment && (
                            <p className="text-sm text-amber-700">
                                Recorded at {new Date(preAssessment.recordedAt).toLocaleString()}
                            </p>
                        )}
                    </>
                )}
                {!sessionId && (
                    <p className="text-sm text-amber-700">
                        Select a session above to begin. Sessions are created when the dialysis machine sends ORU^R01 observations.
                    </p>
                )}
            </div>
        </div>
    );
}
