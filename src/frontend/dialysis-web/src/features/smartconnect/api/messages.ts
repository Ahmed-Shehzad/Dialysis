import { apiClient } from "@/lib/api/apiClient";
import {
  ADMIN_PREFIX,
  type MessageLedgerEntry,
  type MessageListQuery,
  type MessageListResponse,
} from "./types";

export const fetchMessages = async (
  query: MessageListQuery = {},
): Promise<MessageListResponse> => {
  const res = await apiClient.get<MessageListResponse>(`${ADMIN_PREFIX}/messages`, {
    params: query,
  });
  return res.data ?? { items: [], totalCount: 0 };
};

export const fetchMessage = async (id: string): Promise<MessageLedgerEntry> => {
  const res = await apiClient.get<MessageLedgerEntry>(`${ADMIN_PREFIX}/messages/${id}`);
  return res.data;
};

export const reprocessMessage = async (
  id: string,
): Promise<{ reprocessedMessageId: string }> => {
  const res = await apiClient.post<{ reprocessedMessageId: string }>(
    `${ADMIN_PREFIX}/messages/${id}/reprocess`,
  );
  return res.data;
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
