import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { DEMO_PROVIDER_ID, startEncounter } from "@/features/ehr/api/ehrApi";
import {
  COMMON_IMAGING_STUDIES,
  fetchImagingOrders,
  orderImagingStudy,
  reviewImagingAiFinding,
  type ImagingOrder,
  type ImagingOrderStatus,
} from "@/features/imaging/api/imagingApi";
import { StudyPreview } from "@/features/imaging/components/StudyPreview";
import { humanizeError } from "@/lib/api/humanizeError";

const STATUS_TONE: Record<string, string> = {
  Ordered: "border-slate-700 bg-slate-900/40 text-slate-300",
  Scheduled: "border-sky-700/70 bg-sky-950/30 text-sky-100",
  InProgress: "border-amber-700/70 bg-amber-950/30 text-amber-100",
  Completed: "border-emerald-700/70 bg-emerald-950/40 text-emerald-100",
  Cancelled: "border-rose-700/70 bg-rose-950/40 text-rose-100",
};

const statusTone = (status: ImagingOrderStatus): string =>
  STATUS_TONE[status] ?? "border-slate-700 bg-slate-900/40 text-slate-300";

/**
 * EHR chart panel for imaging orders (radiology ↔ DICOM). Lists the patient's imaging orders with
 * modality + status; a completed order shows the linked DICOM study instance UID. The quick-order
 * row chains StartEncounter → OrderImagingStudy (mirrors the labs dialog) for the common dialysis
 * studies. The modality (PACS/RIS via SmartConnect DICOM) fulfils the order and the study is
 * correlated back by accession number.
 */
export const ImagingPanel = ({ patientId }: { patientId: string }) => {
  const queryClient = useQueryClient();
  const [studyIndex, setStudyIndex] = useState(0);

  const orders = useQuery({
    queryKey: ["ehr", "imaging", patientId],
    queryFn: () => fetchImagingOrders(patientId),
    enabled: Boolean(patientId),
    refetchInterval: 30_000,
  });

  const placeOrder = useMutation({
    mutationFn: async () => {
      const study = COMMON_IMAGING_STUDIES[studyIndex];
      if (!study) throw new Error("Select an imaging study first.");
      const encounterId = await startEncounter({
        patientId,
        providerId: DEMO_PROVIDER_ID,
        encounterClassCode: "AMB",
      });
      return orderImagingStudy({
        patientId,
        encounterId,
        orderingProviderId: DEMO_PROVIDER_ID,
        modalityCode: study.modalityCode,
        bodySiteCode: study.bodySiteCode,
      });
    },
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: ["ehr", "imaging", patientId] }),
  });

  const review = useMutation({
    mutationFn: ({ id, accepted }: { id: string; accepted: boolean }) =>
      reviewImagingAiFinding(id, accepted),
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: ["ehr", "imaging", patientId] }),
  });

  const rows = orders.data ?? [];

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header className="mb-2 flex flex-wrap items-end justify-between gap-2">
        <div>
          <h3 className="text-sm font-medium text-slate-200">
            Imaging <span className="text-slate-500">(radiology orders &amp; studies)</span>
          </h3>
          <p className="text-xs text-slate-400">
            Studies are fulfilled by the modality and linked back here once received via DICOM.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <select
            value={studyIndex}
            onChange={(e) => setStudyIndex(Number(e.target.value))}
            aria-label="Imaging study"
            className="rounded border border-slate-700 bg-slate-950 px-2 py-1 text-xs text-slate-100 focus:border-clinic-500 focus:outline-hidden"
          >
            {COMMON_IMAGING_STUDIES.map((s, i) => (
              <option key={s.label} value={i}>
                {s.label}
              </option>
            ))}
          </select>
          <button
            type="button"
            onClick={() => placeOrder.mutate()}
            disabled={placeOrder.isPending}
            className="rounded-md bg-clinic-600 px-3 py-1 text-xs font-medium text-white transition hover:bg-clinic-500 disabled:opacity-50"
          >
            {placeOrder.isPending ? "Ordering…" : "Order imaging"}
          </button>
        </div>
      </header>

      {placeOrder.error && (
        <p role="alert" className="mb-2 text-xs text-rose-300">
          {humanizeError(placeOrder.error)}
        </p>
      )}

      {orders.isLoading && <p className="text-xs text-slate-400">Loading imaging orders…</p>}
      {orders.error && <p className="text-xs text-amber-300">{humanizeError(orders.error)}</p>}
      {!orders.isLoading && rows.length === 0 && (
        <p className="text-xs text-slate-500">No imaging orders on file for this patient.</p>
      )}

      {review.error && (
        <p role="alert" className="mb-2 text-xs text-rose-300">
          {humanizeError(review.error)}
        </p>
      )}

      {rows.length > 0 && (
        <ul className="divide-y divide-slate-800 text-sm">
          {rows.map((order) => (
            <ImagingRow
              key={order.id}
              order={order}
              onReview={(accepted) => review.mutate({ id: order.id, accepted })}
              reviewing={review.isPending}
            />
          ))}
        </ul>
      )}
    </section>
  );
};

const ImagingRow = ({
  order,
  onReview,
  reviewing,
}: {
  order: ImagingOrder;
  onReview: (accepted: boolean) => void;
  reviewing: boolean;
}) => (
  <li className="py-2">
    <div className="grid grid-cols-12 items-center gap-2">
      <span className="col-span-2 font-mono text-xs text-slate-300">{order.modalityCode}</span>
      <span className="col-span-3 truncate text-xs text-slate-200" title={order.bodySiteCode}>
        {order.bodySiteCode}
      </span>
      <span
        className="col-span-3 truncate font-mono text-xs text-slate-500"
        title={order.studyInstanceUid ?? order.accessionNumber}
      >
        {order.studyInstanceUid ? `study ${order.studyInstanceUid}` : order.accessionNumber}
      </span>
      <span className="col-span-2 text-xs text-slate-500">{order.reasonText ?? ""}</span>
      <span className="col-span-2 text-right">
        <span className={`rounded-full border px-2 py-0.5 text-xs ${statusTone(order.status)}`}>
          {order.status}
        </span>
      </span>
    </div>
    {order.studyInstanceUid && (
      <div className="mt-1 pl-[calc(2/12*100%)]">
        <StudyPreview studyInstanceUid={order.studyInstanceUid} />
      </div>
    )}
    {order.aiFindingStatus !== "None" && order.aiFindingDisplay && (
      <AiFindingBlock order={order} onReview={onReview} reviewing={reviewing} />
    )}
  </li>
);

const AiFindingBlock = ({
  order,
  onReview,
  reviewing,
}: {
  order: ImagingOrder;
  onReview: (accepted: boolean) => void;
  reviewing: boolean;
}) => {
  const pending = order.aiFindingStatus === "PendingReview";
  const confidence =
    typeof order.aiFindingConfidence === "number"
      ? ` · ${Math.round(order.aiFindingConfidence * 100)}%`
      : "";
  return (
    <div className="mt-1.5 rounded-md border border-indigo-800/60 bg-indigo-950/30 p-2 text-xs">
      <div className="flex items-center justify-between gap-2">
        <span className="text-indigo-200">
          <span className="mr-1 rounded bg-indigo-800/60 px-1 py-0.5 text-[10px] uppercase tracking-wide">
            AI · advisory
          </span>
          {order.aiFindingDisplay}
          <span className="text-indigo-400">
            {order.aiFindingInterpretation ? ` (${order.aiFindingInterpretation})` : ""}
            {confidence}
          </span>
        </span>
        {pending ? (
          <span className="flex shrink-0 gap-1">
            <button
              type="button"
              onClick={() => onReview(true)}
              disabled={reviewing}
              className="rounded border border-emerald-700/60 px-2 py-0.5 text-emerald-200 transition hover:border-emerald-500 disabled:opacity-50"
            >
              Accept
            </button>
            <button
              type="button"
              onClick={() => onReview(false)}
              disabled={reviewing}
              className="rounded border border-rose-700/60 px-2 py-0.5 text-rose-200 transition hover:border-rose-500 disabled:opacity-50"
            >
              Reject
            </button>
          </span>
        ) : (
          <span className="shrink-0 text-slate-400">
            {order.aiFindingStatus}
            {order.aiReviewedBy ? ` · ${order.aiReviewedBy}` : ""}
          </span>
        )}
      </div>
      {order.aiModelId && (
        <p className="mt-1 text-[11px] text-slate-500">
          model {order.aiModelId} — not a diagnosis; clinician sign-off required
        </p>
      )}
    </div>
  );
};
