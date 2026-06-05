import { apiClient } from "@/lib/api/apiClient";

/**
 * Wire shape returned by the BFF's durable-command status endpoint. Mirrors
 * `Dialysis.BuildingBlocks.DurableCommandBus.AspNetCore.DurableCommandStatusResponse`.
 */
export type DurableCommandStatus = {
  commandId: string;
  correlationId: string;
  status: "Pending" | "Applied" | "Failed";
  enqueuedAtUtc: string;
  appliedAtUtc?: string | null;
  result?: unknown;
  failure?: unknown;
};

/**
 * Wire shape returned by a controller endpoint that publishes via the durable bus.
 * The controllers under PDMS `sessions/{id}/readings` and HIS `integration/device-readings`
 * both return this when their feature flag is on.
 */
export type DurableCommandAcceptance = {
  commandId: string;
  correlationId: string;
  statusEndpoint: string;
  // Slice-specific deterministic-id field — PDMS returns `readingId`, HIS returns `readingId`.
  // Add additional aliases per slice if a future opt-in uses a different field name.
  readingId?: string;
};

export const fetchDurableCommandStatus = async (
  statusEndpoint: string,
): Promise<DurableCommandStatus> => {
  // statusEndpoint is an absolute path (`/api/{module}/api/v1.0/command-status/{correlationId}`)
  // emitted by the publishing controller's Location header; pass-through to the apiClient.
  const response = await apiClient.get<DurableCommandStatus>(statusEndpoint);
  return response.data;
};
