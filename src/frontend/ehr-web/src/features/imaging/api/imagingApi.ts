import { apiClient } from "@/lib/api/apiClient";

type HateoasEnvelope<T> = { data: T; links: unknown[] };
const unwrap = <T>(envelope: HateoasEnvelope<T> | T): T =>
  envelope && typeof envelope === "object" && "data" in (envelope as Record<string, unknown>)
    ? (envelope as HateoasEnvelope<T>).data
    : (envelope as T);

// EHR serialises enums as strings (JsonStringEnumConverter).
export type ImagingOrderStatus =
  | "Ordered"
  | "Scheduled"
  | "InProgress"
  | "Completed"
  | "Cancelled"
  | string;

export type AiFindingReviewStatus = "None" | "PendingReview" | "Accepted" | "Rejected" | string;

export type ImagingOrder = {
  id: string;
  patientId: string;
  accessionNumber: string;
  modalityCode: string;
  bodySiteCode: string;
  reasonText?: string | null;
  status: ImagingOrderStatus;
  studyInstanceUid?: string | null;
  aiFindingStatus: AiFindingReviewStatus;
  aiModelId?: string | null;
  aiFindingDisplay?: string | null;
  aiFindingConfidence?: number | null;
  aiFindingInterpretation?: string | null;
  aiFindingSummary?: string | null;
  aiReviewedBy?: string | null;
};

export type OrderImagingStudyInput = {
  patientId: string;
  encounterId: string;
  orderingProviderId: string;
  modalityCode: string;
  bodySiteCode: string;
  reasonText?: string | null;
};

export const fetchImagingOrders = async (patientId: string, take = 25): Promise<ImagingOrder[]> => {
  const response = await apiClient.get<HateoasEnvelope<ImagingOrder[]> | ImagingOrder[]>(
    `/ehr/api/v1.0/clinical/patients/${patientId}/imaging-orders`,
    { params: { take } },
  );
  return unwrap(response.data);
};

export const orderImagingStudy = async (input: OrderImagingStudyInput): Promise<string> => {
  const response = await apiClient.post<{ id: string }>(
    "/ehr/api/v1.0/clinical/imaging-orders",
    input,
  );
  return response.data.id;
};

/** Human-in-the-loop sign-off on an order's advisory AI finding. */
export const reviewImagingAiFinding = async (
  imagingOrderId: string,
  accepted: boolean,
): Promise<void> => {
  await apiClient.post(
    `/ehr/api/v1.0/clinical/imaging-orders/${imagingOrderId}/ai-finding/review`,
    { accepted },
  );
};

// Common dialysis-relevant imaging studies (DICOM modality + body site).
export const COMMON_IMAGING_STUDIES: ReadonlyArray<{
  label: string;
  modalityCode: string;
  bodySiteCode: string;
}> = [
  { label: "AVF/AVG ultrasound", modalityCode: "US", bodySiteCode: "VascularAccess" },
  { label: "Chest X-ray", modalityCode: "CR", bodySiteCode: "Chest" },
  { label: "Renal ultrasound", modalityCode: "US", bodySiteCode: "Kidney" },
  { label: "Abdominal CT", modalityCode: "CT", bodySiteCode: "Abdomen" },
  { label: "Fistulogram", modalityCode: "XA", bodySiteCode: "VascularAccess" },
];
