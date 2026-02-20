import { useQuery } from "@tanstack/react-query";
import { getPrescriptionCompliance } from "../api";
import { STATS_REFETCH_INTERVAL_MS } from "../constants";
import { getErrorMessage } from "../utils/errorMessage";
import { CardSkeleton } from "./CardSkeleton";

interface Props {
    from?: string;
    to?: string;
}

export function PrescriptionComplianceCard({ from, to }: Readonly<Props>) {
    const { data, error, isLoading, refetch, isRefetching } = useQuery({
        queryKey: ["prescription-compliance", from, to],
        queryFn: () => getPrescriptionCompliance(from, to),
        enabled: Boolean(from ?? to),
        refetchInterval: STATS_REFETCH_INTERVAL_MS,
    });

    const cardBase = "p-5 border border-gray-200 rounded-lg bg-white shadow-sm";
    const cardError =
        "p-5 border border-red-300 rounded-lg bg-red-50 shadow-sm";

    const errorMessage = getErrorMessage(error);
    if (errorMessage)
        return (
            <div className={cardError}>
                Error: {errorMessage}
                <button
                    type="button"
                    onClick={() => refetch()}
                    disabled={isRefetching}
                    className="ml-2 px-2 py-1 rounded text-sm bg-red-200 hover:bg-red-300 disabled:opacity-50"
                >
                    {isRefetching ? "Retrying…" : "Retry"}
                </button>
            </div>
        );
    if (isLoading || !data) return <CardSkeleton />;

    return (
        <div className={cardBase}>
            <h3 className="m-0 mb-4 text-base font-semibold">
                Prescription Compliance
            </h3>
            <p className="my-2">
                <strong>{data.compliancePercent}%</strong> compliant
            </p>
            <p className="my-2">
                {data.compliantCount} / {data.totalEvaluated} sessions
            </p>
            <p className="text-xs text-gray-500 mt-4">
                {new Date(data.from).toLocaleDateString()} –{" "}
                {new Date(data.to).toLocaleDateString()}
            </p>
        </div>
    );
}
