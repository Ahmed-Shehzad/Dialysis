import { apiClient } from "@/lib/api/apiClient";
import {
  ADMIN_PREFIX,
  type MessageLedgerEntry,
  type MessageListQuery,
  type MessageListResponse,
} from "./types";

export const fetchMessages = async (query: MessageListQuery = {}): Promise<MessageListResponse> => {
  const res = await apiClient.get<MessageListResponse>(`${ADMIN_PREFIX}/messages`, {
    params: query,
  });
  return res.data ?? { items: [], totalCount: 0 };
};

export const fetchMessage = async (id: string): Promise<MessageLedgerEntry> => {
  const res = await apiClient.get<MessageLedgerEntry>(`${ADMIN_PREFIX}/messages/${id}`);
  return res.data;
};

export const reprocessMessage = async (id: string): Promise<{ reprocessedMessageId: string }> => {
  const res = await apiClient.post<{ reprocessedMessageId: string }>(
    `${ADMIN_PREFIX}/messages/${id}/reprocess`,
  );
  return res.data;
};

export type ExportFormat = "raw" | "hl7" | "xml" | "cda" | "fhir";

export const EXPORT_FORMATS: { value: ExportFormat; label: string }[] = [
  { value: "raw", label: "Raw text (.txt)" },
  { value: "hl7", label: "HL7 v2 (.hl7)" },
  { value: "xml", label: "HL7 v2 XML (.xml)" },
  { value: "cda", label: "C-CDA / CCD (.cda.xml)" },
  { value: "fhir", label: "FHIR R4 Bundle (.json)" },
];

/**
 * Downloads the ledger entry's captured payload converted to the requested document format.
 * The backend streams a file with a Content-Disposition filename; we honour it and fall back
 * to a sensible default. Auth is carried by the apiClient Bearer interceptor.
 */
export const exportMessageDocument = async (id: string, format: ExportFormat): Promise<void> => {
  const res = await apiClient.get(`${ADMIN_PREFIX}/messages/${id}/export`, {
    params: { format },
    responseType: "blob",
  });
  const disposition = (res.headers["content-disposition"] as string | undefined) ?? "";
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
  const fileName = match?.[1] ? decodeURIComponent(match[1]) : `message-${id}.${format}`;
  const url = URL.createObjectURL(res.data as Blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  URL.revokeObjectURL(url);
};

/**
 * Best-effort decode of the optional base64-encoded payloadSnapshot to text.
 * Returns null if the snapshot wasn't captured or doesn't parse as UTF-8 text.
 * HL7 v2, JSON, and XML payloads are all UTF-8 — binary attachments live
 * separately under /admin/attachments and aren't returned here.
 */
export const decodePayloadSnapshot = (snapshot?: string | null): string | null => {
  if (!snapshot) return null;
  try {
    const bin = atob(snapshot);
    const bytes = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
    return new TextDecoder("utf-8", { fatal: false }).decode(bytes);
  } catch {
    return null;
  }
};
