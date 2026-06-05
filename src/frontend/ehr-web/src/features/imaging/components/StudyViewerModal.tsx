import { useState } from "react";

/**
 * Inline DICOM study viewer. Renders the study's first frame as a PNG produced server-side by the
 * WADO-RS "rendered" endpoint (via the EHR BFF), so the viewer is a plain <img> — no client-side
 * DICOM decoder. Studies whose transfer syntax the server can't decode return 415; we show a quiet
 * "preview unavailable" instead of a broken image.
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
  const [failed, setFailed] = useState(false);
  const renderedUrl = `/ehr/api/_x/dicom/dicom-web/studies/${encodeURIComponent(studyInstanceUid)}/rendered`;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/80 p-4"
      role="dialog"
      aria-modal="true"
      onClick={onClose}
    >
      <div
        className="flex max-h-[90vh] max-w-3xl flex-col overflow-hidden rounded-lg border border-slate-700 bg-slate-900 p-4"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="mb-2 flex items-center justify-between gap-4">
          <div className="min-w-0">
            <h3 className="truncate font-mono text-xs text-slate-400" title={studyInstanceUid}>
              study {studyInstanceUid}
            </h3>
            <p className="text-[11px] text-slate-500">
              {instanceCount} image(s) · showing first frame
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
              Preview unavailable for this study (unsupported transfer syntax). The raw study is
              still retrievable via WADO-RS.
            </p>
          ) : (
            <img
              src={renderedUrl}
              alt="DICOM study preview"
              className="max-h-[72vh] w-auto"
              onError={() => setFailed(true)}
            />
          )}
        </div>
      </div>
    </div>
  );
};
