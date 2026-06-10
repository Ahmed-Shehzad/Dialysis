import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { fetchStudyInstances } from "@/features/imaging/api/dicomApi";

const DICOM_BASE = "/ehr/api/_x/dicom/dicom-web";

/**
 * Inline DICOM study viewer. Pages through every instance under the study, rendering each frame as a
 * PNG produced server-side by the WADO-RS "rendered" endpoint (via the EHR BFF) — so the viewer is a
 * plain <img>, no client-side DICOM decoder. Falls back to the study-level rendered frame if the
 * instance list is unavailable. Frames whose transfer syntax the server can't decode (415) show a
 * quiet "preview unavailable" instead of a broken image.
 */
export const StudyViewerModal = ({
  studyInstanceUid,
  instanceCount,
  onClose,
}: {
  studyInstanceUid: string;
  instanceCount: number;
  onClose: () => void;
}) => {
  const [index, setIndex] = useState(0);
  const [failed, setFailed] = useState(false);
  const encodedStudy = encodeURIComponent(studyInstanceUid);

  const instances = useQuery({
    queryKey: ["ehr", "dicom", "instances", studyInstanceUid],
    queryFn: () => fetchStudyInstances(studyInstanceUid),
    staleTime: 60_000,
  });

  const list = instances.data ?? [];
  const count = list.length || instanceCount;
  const current = list[index];

  // Reset the per-frame error + clamp the index when the list or position changes.
  useEffect(() => setFailed(false), [index, studyInstanceUid]);
  useEffect(() => {
    if (index > 0 && index >= list.length && list.length > 0) setIndex(list.length - 1);
  }, [list.length, index]);

  const renderedUrl = current
    ? `${DICOM_BASE}/studies/${encodedStudy}/series/${encodeURIComponent(current.seriesInstanceUid)}/instances/${encodeURIComponent(current.sopInstanceUid)}/rendered`
    : `${DICOM_BASE}/studies/${encodedStudy}/rendered`;

  const canPage = count > 1;
  const go = (delta: number) =>
    setIndex((i) => Math.min(Math.max(i + delta, 0), Math.max(count - 1, 0)));

  // Escape is the keyboard dismissal path (the backdrop click below is mouse-only).
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    globalThis.addEventListener("keydown", handler);
    return () => globalThis.removeEventListener("keydown", handler);
  }, [onClose]);

  return (
    <div
      role="presentation"
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/80 p-4"
      onClick={(e) => {
        // Backdrop-only dismissal — clicks inside the dialog bubble here with a
        // different target, so they never close it.
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label={`DICOM study viewer — study ${studyInstanceUid}`}
        className="flex max-h-[90vh] max-w-3xl flex-col overflow-hidden rounded-lg border border-slate-700 bg-slate-900 p-4"
      >
        <div className="mb-2 flex items-center justify-between gap-4">
          <div className="min-w-0">
            <h3 className="truncate font-mono text-xs text-slate-400" title={studyInstanceUid}>
              study {studyInstanceUid}
            </h3>
            <p className="text-[11px] text-slate-500">
              image {Math.min(index + 1, Math.max(count, 1))} of {count || 1}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-700 px-2 py-1 text-xs text-slate-200 hover:border-slate-500"
          >
            Close
          </button>
        </div>

        <div className="flex min-h-[12rem] items-center justify-center overflow-auto bg-black">
          {failed ? (
            <p className="p-8 text-sm text-slate-400">
              Preview unavailable for this frame (unsupported transfer syntax). The raw study is
              still retrievable via WADO-RS.
            </p>
          ) : (
            <img
              key={renderedUrl}
              src={renderedUrl}
              alt={`DICOM frame ${index + 1}`}
              className="max-h-[72vh] w-auto"
              onError={() => setFailed(true)}
            />
          )}
        </div>

        {canPage && (
          <div className="mt-3 flex items-center justify-center gap-3 text-sm">
            <button
              type="button"
              onClick={() => go(-1)}
              disabled={index <= 0}
              className="rounded border border-slate-700 px-3 py-1 text-slate-200 hover:border-slate-500 disabled:opacity-40"
            >
              ← Prev
            </button>
            <span className="font-mono text-xs text-slate-500">
              {index + 1} / {count}
            </span>
            <button
              type="button"
              onClick={() => go(1)}
              disabled={index >= count - 1}
              className="rounded border border-slate-700 px-3 py-1 text-slate-200 hover:border-slate-500 disabled:opacity-40"
            >
              Next →
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
