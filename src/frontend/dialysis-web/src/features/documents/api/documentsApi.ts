import { apiClient } from "@/lib/api/apiClient";

export type DocumentStatus = "Current" | "Superseded" | "EnteredInError";
export type DocumentSource = "PdmsReporting" | "HieInbound" | "AdminUpload";
export type SignerKind = "Platform" | "User";

export type DocumentRow = {
  id: string;
  patientId: string;
  kind: string;
  title: string;
  mimeType: string;
  languageCode?: string | null;
  status: DocumentStatus;
  source: DocumentSource;
  size: number;
  createdAtUtc: string;
  signatureCount: number;
  hasAcroForms: boolean;
  hasJavascript: boolean;
};

export type DocumentSignatureRow = {
  id: string;
  signerKind: SignerKind;
  signerUserId?: string | null;
  certThumbprint: string;
  signedAtUtc: string;
  reason?: string | null;
};

export type DocumentDetail = DocumentRow & {
  category?: string | null;
  createdBy?: string | null;
  contentHash: string;
  signatures: DocumentSignatureRow[];
};

type ListFilters = {
  patientId?: string;
  kind?: string;
  status?: DocumentStatus;
  source?: DocumentSource;
  take?: number;
};

type Envelope<T> = { data: T };

export const fetchDocuments = async (filters: ListFilters = {}): Promise<DocumentRow[]> => {
  const response = await apiClient.get<Envelope<DocumentRow[]>>("/api/hie/api/v1.0/documents", {
    params: filters,
  });
  return response.data?.data ?? [];
};

export const fetchDocumentDetail = async (id: string): Promise<DocumentDetail | null> => {
  try {
    const response = await apiClient.get<Envelope<DocumentDetail>>(
      `/api/hie/api/v1.0/documents/${id}`,
    );
    return response.data?.data ?? null;
  } catch (error) {
    if ((error as { response?: { status?: number } }).response?.status === 404) return null;
    throw error;
  }
};

export const documentBinaryUrl = (id: string): string => `/api/hie/api/v1.0/documents/${id}/binary`;

export type UploadDocumentInput = {
  patientId: string;
  kind: string;
  title: string;
  mimeType: string;
  base64Content: string;
  languageCode?: string;
  category?: string;
};

export const uploadDocument = async (input: UploadDocumentInput): Promise<string> => {
  const response = await apiClient.post<Envelope<{ documentId: string }>>(
    "/api/hie/api/v1.0/documents",
    input,
  );
  return response.data.data.documentId;
};

export type SignDocumentInput = {
  certificateSource: SignerKind;
  userId?: string;
  reason?: string;
  location?: string;
  contactInfo?: string;
};

export const signDocument = async (id: string, input: SignDocumentInput): Promise<string> => {
  const response = await apiClient.post<Envelope<{ documentId: string }>>(
    `/api/hie/api/v1.0/documents/${id}/sign`,
    input,
  );
  return response.data.data.documentId;
};

export const deleteDocument = async (id: string): Promise<void> => {
  await apiClient.delete(`/api/hie/api/v1.0/documents/${id}`);
};

export const arrayBufferToBase64 = (buffer: ArrayBuffer): string => {
  let binary = "";
  const bytes = new Uint8Array(buffer);
  const chunk = 0x8000;
  for (let i = 0; i < bytes.length; i += chunk) {
    binary += String.fromCharCode.apply(null, Array.from(bytes.subarray(i, i + chunk)));
  }
  return btoa(binary);
};
