import { apiClient } from "@/lib/api/apiClient";
import { ADMIN_PREFIX } from "./types";

// HL7 Workbench — operator-facing parse / validate / dispatch for user-provided HL7 v2 payloads.
// The backend is intentionally stateless: every endpoint takes the raw HL7 text in the request body
// and returns a structured response. No canned data is hosted anywhere on the server.

export type WorkbenchParsedHeader = {
  sendingApp: string | null;
  sendingFacility: string | null;
  receivingApp: string | null;
  receivingFacility: string | null;
  timestamp: string | null;
  messageType: string | null;
  trigger: string | null;
  controlId: string | null;
  processingId: string | null;
  version: string | null;
};

export type WorkbenchParseResponse = {
  header: WorkbenchParsedHeader;
  segmentsJson: string;
  segmentNames: string[];
};

export type WorkbenchValidateRequest = {
  payloadText: string;
  requiredSegments?: string[];
  minVersion?: string;
};

export type WorkbenchValidateResponse = {
  isValid: boolean;
  reason: string | null;
  header: {
    trigger: string | null;
    version: string | null;
    controlId: string | null;
  } | null;
  segmentsJson: string | null;
};

export type WorkbenchDispatchResponse = {
  dispatchedMessageId: string;
  correlationId: string;
  succeeded: boolean;
  error: string | null;
  outboundRoutesAttempted: number[];
  responsePayload: string | null;
  ledgerSnapshot: Array<{
    id: string;
    status: string;
    outboundRouteOrdinal: number | null;
    detail: string | null;
    createdAtUtc: string;
  }>;
};

const WORKBENCH_PREFIX = `${ADMIN_PREFIX}/workbench`;

export const workbenchParseHl7 = async (payloadText: string): Promise<WorkbenchParseResponse> => {
  const res = await apiClient.post<WorkbenchParseResponse>(`${WORKBENCH_PREFIX}/parse-hl7`, {
    payloadText,
  });
  return res.data;
};

export const workbenchValidateHl7 = async (
  body: WorkbenchValidateRequest,
): Promise<WorkbenchValidateResponse> => {
  const res = await apiClient.post<WorkbenchValidateResponse>(
    `${WORKBENCH_PREFIX}/validate-hl7`,
    body,
  );
  return res.data;
};

export const workbenchDispatch = async (
  flowId: string,
  payloadText: string,
): Promise<WorkbenchDispatchResponse> => {
  const res = await apiClient.post<WorkbenchDispatchResponse>(`${WORKBENCH_PREFIX}/dispatch`, {
    flowId,
    payloadText,
  });
  return res.data;
};
