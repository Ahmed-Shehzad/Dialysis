import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  fetchPendingReviews,
  resolveReview,
  type PatientLinkReview,
} from "@/features/mpi/api/mpiApi";
import { humanizeError } from "@/lib/api/humanizeError";

const GRADE_TONE: Record<string, string> = {
  Certain: "border-rose-700/70 bg-rose-950/40 text-rose-100",
  Probable: "border-amber-700/70 bg-amber-950/30 text-amber-100",
  Possible: "border-slate-700 bg-slate-900/40 text-slate-300",
};

const gradeTone = (grade: string): string =>
  GRADE_TONE[grade] ?? "border-slate-700 bg-slate-900/40 text-slate-300";

/**
 * Master Patient Index steward console: the probable-duplicate review queue. Each row is a candidate
 * pair the probabilistic matcher flagged on inbound ingestion — strong enough to suspect the same
 * person across sources, not strong enough to auto-link. The steward links (same person) or rejects
 * (distinct). Resolving a pair removes it from the queue.
 */
export const MpiStewardPage = () => {
  const queryClient = useQueryClient();
  const reviews = useQuery({
    queryKey: ["hie", "mpi", "reviews"],
    queryFn: () => fetchPendingReviews(200),
    refetchInterval: 30_000,
  });

  const resolve = useMutation({
    mutationFn: ({ id, link }: { id: string; link: boolean }) => resolveReview(id, link),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["hie", "mpi", "reviews"] }),
  });

  const rows = reviews.data ?? [];

  return (
    <div className="space-y-4">
      <header>
        <h1 className="text-lg font-semibold text-slate-100">MPI duplicate review queue</h1>
        <p className="text-sm text-slate-400">
          Probable cross-source patient matches awaiting steward adjudication. Link confirms the
          same person; reject marks them distinct. Nothing is auto-linked.
        </p>
      </header>

      {reviews.isLoading && <p className="text-sm text-slate-400">Loading review queue…</p>}
      {reviews.error && <p className="text-sm text-rose-300">{humanizeError(reviews.error)}</p>}
      {!reviews.isLoading && rows.length === 0 && (
        <p className="rounded-md border border-dashed border-slate-700 p-4 text-sm text-slate-500">
          No pending duplicate reviews. The queue fills as the matcher flags probable cross-source
          duplicates on inbound ingestion.
        </p>
      )}

      {resolve.error && (
        <p role="alert" className="text-xs text-rose-300">
          {humanizeError(resolve.error)}
        </p>
      )}

      {rows.length > 0 && (
        <ul className="space-y-2">
          {rows.map((r) => (
            <ReviewRow
              key={r.id}
              review={r}
              onResolve={(link) => resolve.mutate({ id: r.id, link })}
              busy={resolve.isPending}
            />
          ))}
        </ul>
      )}
    </div>
  );
};

const ReviewRow = ({
  review,
  onResolve,
  busy,
}: {
  review: PatientLinkReview;
  onResolve: (link: boolean) => void;
  busy: boolean;
}) => (
  <li className="rounded-lg border border-slate-800 bg-slate-900/40 p-3">
    <div className="flex flex-wrap items-center justify-between gap-2">
      <span className={`rounded-full border px-2 py-0.5 text-xs ${gradeTone(review.grade)}`}>
        {review.grade} · {Math.round(review.score * 100)}%
      </span>
      <span className="flex gap-2">
        <button
          type="button"
          onClick={() => onResolve(true)}
          disabled={busy}
          className="rounded-md border border-emerald-700/60 px-3 py-1 text-xs text-emerald-200 transition hover:border-emerald-500 disabled:opacity-50"
        >
          Link (same person)
        </button>
        <button
          type="button"
          onClick={() => onResolve(false)}
          disabled={busy}
          className="rounded-md border border-rose-700/60 px-3 py-1 text-xs text-rose-200 transition hover:border-rose-500 disabled:opacity-50"
        >
          Reject (distinct)
        </button>
      </span>
    </div>
    <div className="mt-2 grid gap-2 sm:grid-cols-2">
      <RecordCard heading="Incoming" label={review.sourceLabel} partner={review.sourcePartnerId} />
      <RecordCard
        heading="Existing candidate"
        label={review.candidateLabel}
        partner={review.candidatePartnerId}
      />
    </div>
  </li>
);

const RecordCard = ({
  heading,
  label,
  partner,
}: {
  heading: string;
  label: string;
  partner: string;
}) => (
  <div className="rounded-md border border-slate-800 bg-slate-950/40 p-2">
    <p className="text-[11px] uppercase tracking-wide text-slate-500">{heading}</p>
    <p className="text-sm text-slate-200">{label}</p>
    <p className="font-mono text-[11px] text-slate-500">{partner}</p>
  </div>
);
