import { apiClient } from "@/lib/api/apiClient";

export type DocumentStatus = "Current" | "Superseded" | "EnteredInError";
export type DocumentSource = "PdmsReporting" | "HieInbound" | "AdminUpload" | "Billing";
export type SignerKind = "Platform" | "User" | "RemoteQes";

/** PAdES conformance level — drives whether a TSA timestamp + DSS is embedded. */
export type PadesLevel = "B" | "T" | "LT" | "LTA";

/** Whether the signature is an advanced or a qualified electronic signature. */
export type SignatureFormat = "Aes" | "Qes";

/** What revocation evidence the signature row carries in its DSS dictionary. */
export type RevocationEvidenceFormat = "None" | "Crl" | "Ocsp" | "Both";

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
  /** Free-form correlation key (e.g. the originating session id for invoices). */
  category?: string | null;
};

export type DocumentSignatureRow = {
  id: string;
  signerKind: SignerKind;
  signerUserId?: string | null;
  certThumbprint: string;
  signedAtUtc: string;
  reason?: string | null;
  padesLevel: PadesLevel;
  signatureFormat: SignatureFormat;
  tsaUri?: string | null;
  timestampedAtUtc?: string | null;
  revocationEvidenceFormat: RevocationEvidenceFormat;
  tspId?: string | null;
  tspCredentialId?: string | null;
};

export type DocumentDetail = DocumentRow & {
  category?: string | null;
  createdBy?: string | null;
  contentHash: string;
  /** Per-document gate the SPA viewer consults to enable pdfjs JS execution. */
  allowJavaScriptExecution: boolean;
  signatures: DocumentSignatureRow[];
};

export type DocumentPreviewFormat = "Pdf" | "Xml" | "Text" | "Binary";

export type DocumentPreview = {
  format: DocumentPreviewFormat;
  content?: string | null;
  mimeType: string;
  rootElement?: string | null;
  documentTypeName?: string | null;
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
  const response = await apiClient.get<Envelope<DocumentRow[]>>("/pdms/api/_x/hie/api/v1.0/documents", {
    params: filters,
  });
  return response.data?.data ?? [];
};

export const fetchDocumentDetail = async (id: string): Promise<DocumentDetail | null> => {
  try {
    const response = await apiClient.get<Envelope<DocumentDetail>>(
      `/pdms/api/_x/hie/api/v1.0/documents/${id}`,
    );
    return response.data?.data ?? null;
  } catch (error) {
    if ((error as { response?: { status?: number } }).response?.status === 404) return null;
    throw error;
  }
};

export const documentBinaryUrl = (id: string): string => `/pdms/api/_x/hie/api/v1.0/documents/${id}/binary`;

/**
 * Downloads the raw document bytes through the authenticated apiClient (so the Bearer token +
 * credentials are attached) and saves them via a transient object URL. Plain `<a href>` links to
 * the `/binary` endpoint can't carry the Authorization header and 401 against the module API.
 */
export const downloadDocumentBinary = async (id: string, filename: string): Promise<void> => {
  const response = await apiClient.get<Blob>(documentBinaryUrl(id), { responseType: "blob" });
  const url = URL.createObjectURL(response.data);
  try {
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = filename;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
  } finally {
    URL.revokeObjectURL(url);
  }
};

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
    "/pdms/api/_x/hie/api/v1.0/documents",
    input,
  );
  return response.data.data.documentId;
};

export type SignDocumentInput = {
  certificateSource: SignerKind;
  /** PAdES conformance level — defaults to "B" when omitted (server-side). */
  level?: PadesLevel;
  /** TSP credential id — required when `certificateSource === "RemoteQes"`. */
  tspCredentialId?: string;
  userId?: string;
  reason?: string;
  location?: string;
  contactInfo?: string;
};

export const signDocument = async (id: string, input: SignDocumentInput): Promise<string> => {
  const response = await apiClient.post<Envelope<{ documentId: string }>>(
    `/pdms/api/_x/hie/api/v1.0/documents/${id}/sign`,
    input,
  );
  return response.data.data.documentId;
};

export const deleteDocument = async (id: string): Promise<void> => {
  await apiClient.delete(`/pdms/api/_x/hie/api/v1.0/documents/${id}`);
};

export type FillDocumentResult = {
  documentId: string;
  filledFieldNames: string[];
  unknownFields: string[];
};

export const fillDocumentAcroForm = async (
  id: string,
  fieldValues: Record<string, string>,
): Promise<FillDocumentResult> => {
  const response = await apiClient.post<Envelope<FillDocumentResult>>(
    `/pdms/api/_x/hie/api/v1.0/documents/${id}/fill`,
    { fieldValues },
  );
  return response.data.data;
};

export const setJavaScriptExecution = async (id: string, allow: boolean): Promise<boolean> => {
  const response = await apiClient.post<
    Envelope<{ documentId: string; allowJavaScriptExecution: boolean }>
  >(`/pdms/api/_x/hie/api/v1.0/documents/${id}/javascript-execution`, { allow });
  return response.data.data.allowJavaScriptExecution;
};

export const fetchDocumentPreview = async (id: string): Promise<DocumentPreview | null> => {
  try {
    const response = await apiClient.get<Envelope<DocumentPreview>>(
      `/pdms/api/_x/hie/api/v1.0/documents/${id}/preview`,
    );
    return response.data?.data ?? null;
  } catch (error) {
    if ((error as { response?: { status?: number } }).response?.status === 404) return null;
    throw error;
  }
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
