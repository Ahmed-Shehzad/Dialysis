import { apiClient } from "@/lib/api/apiClient";

export type DemoResetResult = {
  reseeded: boolean;
  sessions: number;
};

/**
 * Wipes all dialysis sessions and repaints the demo snapshot (1 in-progress, 1 paused,
 * 2 scheduled-for-stage). Dev-only: the endpoint 404s when `Pdms:Demo:Enabled` is off.
 */
export const resetDemoSessions = async (): Promise<DemoResetResult> => {
  const response = await apiClient.post<DemoResetResult>("/api/pdms/api/v1.0/demo/reset-sessions");
  return response.data;
};
