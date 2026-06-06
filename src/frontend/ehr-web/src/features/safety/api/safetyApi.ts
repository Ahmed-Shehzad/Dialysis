import { apiClient } from "@/lib/api/apiClient";

const prefix = "/ehr/api/v1.0/safety/surveillance";

export type SurveillanceBucket = {
  kind: string;
  severity: string;
  count: number;
};

export type SurveillanceSpike = {
  kind: string;
  currentCount: number;
  baselineCount: number;
};

export type SurveillanceResult = {
  windowDays: number;
  total: number;
  buckets: SurveillanceBucket[];
  spikes: SurveillanceSpike[];
};

/** Adverse-event surveillance snapshot over a window (counts by kind/severity + spike flags). */
export const fetchSurveillance = async (
  windowDays = 7,
  take = 500,
): Promise<SurveillanceResult> => {
  const response = await apiClient.get<SurveillanceResult>(prefix, {
    params: { windowDays, take },
  });
  return response.data ?? { windowDays, total: 0, buckets: [], spikes: [] };
};
