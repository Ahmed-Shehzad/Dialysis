import { useQuery } from "@tanstack/react-query";
import { getSessionsSummary } from "../api";
import { CardSkeleton } from "./CardSkeleton";

interface Props {
    from?: string;
    to?: string;
}

export function SessionsSummaryCard({ from, to }: Readonly<Props>) {
    const { data, error, isLoading, refetch, isRefetching } = useQuery({
        queryKey: ["sessions-summary", from, to],
        queryFn: () => getSessionsSummary(from, to),
        enabled: Boolean(from ?? to),
    });

    const cardBase = "p-5 border border-gray-200 rounded-lg bg-white shadow-sm";
    const cardError =
        "p-5 border border-red-300 rounded-lg bg-red-50 shadow-sm";

    const errorMessage =
        error instanceof Error ? error.message : error ? String(error) : null;
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
                Sessions Summary
            </h3>
            <p className="my-2">
                <strong>{data.sessionCount}</strong> sessions
            </p>
            <p className="my-2">
                Avg duration:{" "}
                <strong>{data.avgDurationMinutes.toFixed(1)}</strong> min
            </p>
            <p className="text-xs text-gray-500 mt-4">
                {new Date(data.from).toLocaleDateString()} –{" "}
                {new Date(data.to).toLocaleDateString()}
            </p>
        </div>
    );
}
