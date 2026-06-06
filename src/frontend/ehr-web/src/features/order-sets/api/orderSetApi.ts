import { apiClient } from "@/lib/api/apiClient";
import { type SafetyAdvisory, SafetyBlockedError } from "@/features/ehr/api/ehrApi";

const prefix = "/ehr/api/v1.0/order-sets";

export type OrderSetSummary = {
  id: string;
  name: string;
  description?: string | null;
  labLines: number;
  medicationLines: number;
  imagingLines: number;
};

export type AppliedOrder = { kind: string; orderId: string };
export type ApplyOrderSetResult = { orders: AppliedOrder[]; advisories: SafetyAdvisory[] };

export const fetchOrderSets = async (): Promise<OrderSetSummary[]> => {
  const response = await apiClient.get<OrderSetSummary[]>(prefix);
  return response.data ?? [];
};

/** Applies an order set; translates a 422 (blocking advisory on a line) into a SafetyBlockedError. */
export const applyOrderSet = async (
  orderSetId: string,
  body: {
    patientId: string;
    encounterId: string;
    orderingProviderId: string;
    acknowledgeAdvisories?: boolean;
    overrideReason?: string;
  },
): Promise<ApplyOrderSetResult> => {
  try {
    const response = await apiClient.post<ApplyOrderSetResult>(
      `${prefix}/${orderSetId}/apply`,
      body,
    );
    return { orders: response.data.orders ?? [], advisories: response.data.advisories ?? [] };
  } catch (error) {
    const res = (
      error as { response?: { status?: number; data?: { advisories?: SafetyAdvisory[] } } }
    ).response;
    if (res?.status === 422) throw new SafetyBlockedError(res.data?.advisories ?? []);
    throw error;
  }
};
