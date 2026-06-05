import { useQuery } from "@tanstack/react-query";
import { fetchStudyMetadata } from "@/features/imaging/api/dicomApi";

/**
 * Inline preview badge for a linked DICOM study on the chart Imaging panel: shows the modality and
 * number of images pulled from DICOMweb QIDO-RS (via the EHR BFF). Best-effort — until the study has
 * actually been received into the DICOM store it renders a quiet "not yet received" hint.
 */
export const StudyPreview = ({ studyInstanceUid }: { studyInstanceUid: string }) => {
  const query = useQuery({
    queryKey: ["ehr", "dicom", "study", studyInstanceUid],
    queryFn: () => fetchStudyMetadata(studyInstanceUid),
    staleTime: 60_000,
    retry: 1,
  });

  if (query.isLoading) {
    return <span className="text-[11px] text-slate-500">loading study…</span>;
  }

  const meta = query.data;
  if (!meta) {
    return <span className="text-[11px] text-slate-600">study not yet received</span>;
  }

  return (
    <span className="inline-flex items-center gap-1 rounded border border-slate-700 bg-slate-900/40 px-1.5 py-0.5 text-[11px] text-slate-300">
      {meta.modality && <span className="font-mono">{meta.modality}</span>}
      <span>
        · {meta.instanceCount} image{meta.instanceCount === 1 ? "" : "s"}
      </span>
    </span>
  );
};
