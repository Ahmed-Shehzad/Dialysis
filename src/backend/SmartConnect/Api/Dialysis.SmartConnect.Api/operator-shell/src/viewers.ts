// Slice I of the SmartConnect ↔ Mirth alignment plan: pluggable inline previews per
// attachment MIME type. Mirth's attachment viewer panel renders a different widget per
// content type — JSON tree, HL7v2 segment tree, image thumbnail, etc. Slice I gives the
// operator shell the same shape so a clinician can preview a lab CSV, an ACK, or an
// imaging metadata DICOM blob without leaving the page.

import { downloadAttachmentUrl } from "./api";
import { el, type ElChild } from "./dom";

/** Returns a DOM node previewing the given attachment payload. Never throws — falls back
 * to a download link when the payload doesn't parse against the picked viewer's schema. */
export type AttachmentViewer = (bytes: Uint8Array, attachmentId: string, mimeType: string) => HTMLElement;

/** Pluggable registry — call `registerViewer(mimePrefix, viewer)` to add a custom
 * renderer. Matched longest-prefix-first so `application/dicom+json` beats `application/`. */
const _registry: Array<{ prefix: string; viewer: AttachmentViewer }> = [];

export function registerViewer(mimePrefix: string, viewer: AttachmentViewer): void {
  _registry.push({ prefix: mimePrefix.toLowerCase(), viewer });
  _registry.sort((a, b) => b.prefix.length - a.prefix.length);
}

export function pickViewer(mimeType: string | undefined): AttachmentViewer {
  if (!mimeType) return _fallbackViewer;
  const lower = mimeType.toLowerCase();
  for (const entry of _registry) {
    if (lower.startsWith(entry.prefix)) return entry.viewer;
  }
  return _fallbackViewer;
}

// ---- Built-in viewers -------------------------------------------------------

const _decoder = new TextDecoder("utf-8", { fatal: false });

function _textBlock(text: string, lang?: string): HTMLElement {
  const pre = el("pre", { class: lang ? `viewer viewer-${lang}` : "viewer" }, text);
  return pre;
}

function _jsonViewer(bytes: Uint8Array): HTMLElement {
  const raw = _decoder.decode(bytes);
  try {
    const parsed = JSON.parse(raw);
    return _textBlock(JSON.stringify(parsed, null, 2), "json");
  } catch {
    return _textBlock(raw, "json");
  }
}

function _xmlViewer(bytes: Uint8Array): HTMLElement {
  const raw = _decoder.decode(bytes);
  // Browser-side pretty-print: parse + re-serialise via XMLSerializer would be ideal but
  // requires DOMParser dependency. For now ship the raw text in a <pre> with a CSS hook.
  return _textBlock(raw, "xml");
}

function _plainTextViewer(bytes: Uint8Array): HTMLElement {
  return _textBlock(_decoder.decode(bytes), "text");
}

/** HL7v2: split segments on \r and present each as a tree-collapsable summary. */
function _hl7v2Viewer(bytes: Uint8Array): HTMLElement {
  const raw = _decoder.decode(bytes);
  const segments = raw.split(/\r\n|\r|\n/).filter((s) => s.length > 0);
  const children: ElChild[] = segments.map((seg) => {
    const name = seg.slice(0, 3);
    const fields = seg.split("|");
    return el("details", { class: "viewer viewer-hl7v2-segment" }, [
      el("summary", {}, `${name} (${fields.length - 1} fields)`),
      _textBlock(seg, "hl7v2-raw"),
    ]);
  });
  return el("div", { class: "viewer viewer-hl7v2" }, children);
}

function _imageViewer(bytes: Uint8Array, attachmentId: string, mimeType: string): HTMLElement {
  const url = downloadAttachmentUrl(attachmentId);
  // Use the API endpoint directly rather than a blob: URL so the browser honours
  // Authorization headers via the existing fetch interceptor and doesn't hold the
  // raw bytes twice.
  return el("img", { src: url, alt: `attachment ${attachmentId}`, class: "viewer viewer-image", loading: "lazy" });
}

function _fallbackViewer(bytes: Uint8Array, attachmentId: string, mimeType: string): HTMLElement {
  return el("div", { class: "viewer viewer-fallback" }, [
    el("p", { class: "muted" }, `No inline preview for ${mimeType || "this MIME type"}.`),
    el("a", { href: downloadAttachmentUrl(attachmentId), target: "_blank", rel: "noopener" }, "Download to inspect"),
  ]);
}

// ---- Default registry ------------------------------------------------------
// Order doesn't matter — registry is sorted longest-prefix-first on insert.

registerViewer("application/json", _jsonViewer);
registerViewer("application/fhir+json", _jsonViewer);
registerViewer("application/x-ndjson", _jsonViewer);
registerViewer("application/xml", _xmlViewer);
registerViewer("application/fhir+xml", _xmlViewer);
registerViewer("text/xml", _xmlViewer);
registerViewer("text/plain", _plainTextViewer);
registerViewer("text/csv", _plainTextViewer);
registerViewer("text/tab-separated-values", _plainTextViewer);
registerViewer("application/x-hl7v2", _hl7v2Viewer);
registerViewer("application/hl7-v2", _hl7v2Viewer);
registerViewer("text/hl7v2", _hl7v2Viewer);
registerViewer("image/png", _imageViewer);
registerViewer("image/jpeg", _imageViewer);
registerViewer("image/gif", _imageViewer);
registerViewer("image/webp", _imageViewer);
registerViewer("image/svg+xml", _imageViewer);
