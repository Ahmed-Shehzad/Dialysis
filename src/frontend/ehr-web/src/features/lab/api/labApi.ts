import { apiClient } from "@/lib/api/apiClient";

// The headless Lab bounded context is reached through the EHR BFF's _x/lab aggregation:
//   /ehr/api/_x/lab/{rest} → {labApi}/{rest}  (verbatim prefix strip)
// so every Lab call below is the module's own route with the aggregation prefix in front.
const LAB_BASE = "/ehr/api/_x/lab/api/v1.0/lab";

type HateoasEnvelope<T> = { data: T; links: unknown[] };
const unwrap = <T>(envelope: HateoasEnvelope<T> | T): T =>
  envelope && typeof envelope === "object" && "data" in (envelope as Record<string, unknown>)
    ? (envelope as HateoasEnvelope<T>).data
    : (envelope as T);

// The Lab API serialises its enums as strings (JsonStringEnumConverter), so the SPA renders
// them directly. Kept as string unions for editor help without coupling to the exact set.
export type LabOrderPriority = "Routine" | "Stat" | string;
export type LabOrderStatus =
  | "Placed"
  | "Transmitted"
  | "InProgress"
  | "Resulted"
  | "Cancelled"
  | string;
export type LabResultInterpretation =
  | "Normal"
  | "Low"
  | "High"
  | "CriticalLow"
  | "CriticalHigh"
  | "Abnormal"
  | string;

export type LabOrderSummary = {
  id: string;
  placerOrderNumber: string;
  priority: LabOrderPriority;
  status: LabOrderStatus;
  testCount: number;
  placedAtUtc: string;
  resultedAtUtc?: string | null;
};

export type LabObservation = {
  loincCode: string;
  display: string;
  value: string;
  unit?: string | null;
  referenceRange?: string | null;
  interpretation: LabResultInterpretation;
};

export type LabTestRequest = {
  loincCode: string;
  display: string;
};

export type LabOrderDetail = {
  id: string;
  patientId: string;
  placerOrderNumber: string;
  fillerOrderNumber?: string | null;
  priority: LabOrderPriority;
  status: LabOrderStatus;
  specimen?: string | null;
  placedBy: string;
  placedAtUtc: string;
  resultedAtUtc?: string | null;
  tests: LabTestRequest[];
  results: LabObservation[];
};

export const fetchLabOrdersByPatient = async (
  patientId: string,
  take = 25,
): Promise<LabOrderSummary[]> => {
  const response = await apiClient.get<HateoasEnvelope<LabOrderSummary[]> | LabOrderSummary[]>(
    `${LAB_BASE}/orders/by-patient/${patientId}`,
    { params: { take } },
  );
  return unwrap(response.data);
};

export const fetchLabOrder = async (orderId: string): Promise<LabOrderDetail> => {
  const response = await apiClient.get<HateoasEnvelope<LabOrderDetail> | LabOrderDetail>(
    `${LAB_BASE}/orders/${orderId}`,
  );
  return unwrap(response.data);
};
