import { useQuery } from "@tanstack/react-query";
import { getPrescriptionByMrn } from "../api";

interface PrescriptionModalProps {
    patientMrn: string;
    onClose: () => void;
}

export function PrescriptionModal({ patientMrn, onClose }: Readonly<PrescriptionModalProps>) {
    const { data, error, isLoading } = useQuery({
        queryKey: ["prescription", patientMrn],
        queryFn: () => getPrescriptionByMrn(patientMrn),
        enabled: Boolean(patientMrn),
    });

    return (
        <div
            className="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
            role="dialog"
            aria-modal="true"
            aria-labelledby="prescription-modal-title"
            onClick={(e) => e.target === e.currentTarget && onClose()}
        >
            <div
                className="max-w-md w-full mx-4 rounded-lg border border-slate-200 bg-white shadow-xl"
                onClick={(e) => e.stopPropagation()}
            >
                <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
                    <h2 id="prescription-modal-title" className="m-0 text-lg font-semibold text-slate-800">
                        Prescription (MRN: {patientMrn})
                    </h2>
                    <button
                        type="button"
                        onClick={onClose}
                        className="rounded p-1 text-slate-500 hover:bg-slate-100 hover:text-slate-700"
                        aria-label="Close"
                    >
                        ✕
                    </button>
                </div>
                <div className="p-4">
                    {isLoading && (
                        <p className="text-sm text-slate-500">Loading prescription…</p>
                    )}
                    {error && (
                        <p className="text-sm text-red-600">
                            Failed to load prescription: {error instanceof Error ? error.message : String(error)}
                        </p>
                    )}
                    {data && (
                        <dl className="grid gap-2 text-sm">
                            <div>
                                <dt className="text-slate-500">Order ID</dt>
                                <dd className="font-medium text-slate-800">{data.orderId}</dd>
                            </div>
                            <div>
                                <dt className="text-slate-500">Therapy modality</dt>
                                <dd className="font-medium text-slate-800">{data.therapyModality}</dd>
                            </div>
                            <div>
                                <dt className="text-slate-500">Blood flow rate</dt>
                                <dd className="font-medium text-slate-800">
                                    {data.bloodFlowRateMlMin != null ? `${data.bloodFlowRateMlMin} mL/min` : "—"}
                                </dd>
                            </div>
                            <div>
                                <dt className="text-slate-500">UF target volume</dt>
                                <dd className="font-medium text-slate-800">
                                    {data.ufTargetVolumeMl != null ? `${data.ufTargetVolumeMl} mL` : "—"}
                                </dd>
                            </div>
                            <div>
                                <dt className="text-slate-500">UF rate</dt>
                                <dd className="font-medium text-slate-800">
                                    {data.ufRateMlH != null ? `${data.ufRateMlH} mL/h` : "—"}
                                </dd>
                            </div>
                        </dl>
                    )}
                    {!isLoading && !error && data == null && (
                        <p className="text-sm text-slate-500">No prescription found for this patient.</p>
                    )}
                </div>
                <div className="border-t border-slate-200 px-4 py-3">
                    <button
                        type="button"
                        onClick={onClose}
                        className="rounded bg-slate-700 px-4 py-2 text-sm font-medium text-white hover:bg-slate-800"
                    >
                        Close
                    </button>
                </div>
            </div>
        </div>
    );
}
