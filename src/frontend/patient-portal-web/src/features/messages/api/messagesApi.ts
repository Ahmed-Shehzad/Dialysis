import { apiClient } from "@/lib/api/apiClient";

const prefix = "/portal/api/_x/ehr/api/v1.0/portal/messages";

export type MessageThread = {
  threadId: string;
  subject: string;
  lastMessageAtUtc: string;
  lastDirection: string;
  messageCount: number;
  unreadFromCareTeam: number;
};

export type SecureMessage = {
  id: string;
  threadId: string;
  patientId: string;
  direction: string;
  subject: string;
  body: string;
  sentAtUtc: string;
  readAtUtc?: string | null;
};

/** The patient's own message threads (inbox), newest activity first. */
export const fetchMyThreads = async (patientId: string): Promise<MessageThread[]> => {
  const response = await apiClient.get<MessageThread[]>(`${prefix}/patients/${patientId}/threads`);
  return response.data ?? [];
};

/** Messages in one of the patient's threads, oldest first. */
export const fetchThreadMessages = async (
  patientId: string,
  threadId: string,
): Promise<SecureMessage[]> => {
  const response = await apiClient.get<SecureMessage[]>(
    `${prefix}/patients/${patientId}/threads/${threadId}`,
  );
  return response.data ?? [];
};

/** Patient sends a message — omit threadId to start a new conversation. */
export const sendMessage = async (
  patientId: string,
  body: { threadId?: string; targetProviderId?: string; subject: string; body: string },
): Promise<{ id: string }> => {
  const response = await apiClient.post<{ id: string }>(`${prefix}/patients/${patientId}`, body);
  return response.data;
};

/** Marks a care-team message read (clears the unread badge). */
export const markMessageRead = async (patientId: string, messageId: string): Promise<void> => {
  await apiClient.post(`${prefix}/patients/${patientId}/messages/${messageId}/read`);
};
