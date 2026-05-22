// Slice H of the SmartConnect ↔ Mirth alignment plan: an inline DICOM preview for the
// operator shell's Attachments panel. Registers with slice I's viewer registry
// (registerViewer) and shows a tag table extracted from the DICOM file's metadata.
// Full pixel-data rendering is deliberately out of scope — that's a PACS workflow;
// this viewer answers "is this the right file, and which patient/study is it for?"
// without leaving the page.

import { downloadAttachmentUrl } from "./api";
import { el, type ElChild } from "./dom";
import { registerViewer } from "./viewers";

/** A handful of well-known DICOM tags that operators need at a glance when triaging
 * imaging-attachment routing. The map is intentionally small — operators run an actual
 * PACS for the full tag dump and image rendering. */
const _wellKnownTags: Readonly<Record<string, string>> = {
  "00080016": "SOPClassUID",
  "00080018": "SOPInstanceUID",
  "00080020": "StudyDate",
  "00080030": "StudyTime",
  "00080050": "AccessionNumber",
  "00080060": "Modality",
  "00080070": "Manufacturer",
  "00080090": "ReferringPhysicianName",
  "00081030": "StudyDescription",
  "00100010": "PatientName",
  "00100020": "PatientID",
  "00100030": "PatientBirthDate",
  "00100040": "PatientSex",
  "0020000D": "StudyInstanceUID",
  "0020000E": "SeriesInstanceUID",
  "00200010": "StudyID",
  "00200011": "SeriesNumber",
  "00280010": "Rows",
  "00280011": "Columns",
  "7FE00010": "PixelData",
};

/** Variable-length value-representations need a 32-bit length field; the rest use
 * 16-bit. Per DICOM PS3.5 Table 7.1-1. */
const _longLengthVrs = new Set(["OB", "OW", "OF", "OD", "OL", "SQ", "UT", "UN"]);

interface DicomElement {
  tag: string; // 8-char hex GGGGEEEE
  vr: string;
  length: number;
  /** 0-based offset of the value bytes within the input buffer. */
  valueOffset: number;
}

/** Walks the explicit-VR little-endian stream. Returns null when the bytes don't
 * look like DICOM (missing preamble + DICM magic). Never throws — corrupt files
 * just yield however many elements were readable before the walk fell off the end. */
function _parseDicom(bytes: Uint8Array): DicomElement[] | null {
  if (bytes.length < 132) return null;
  if (bytes[128] !== 0x44 || bytes[129] !== 0x49 || bytes[130] !== 0x43 || bytes[131] !== 0x4D) {
    return null; // missing "DICM" magic
  }
  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  const elements: DicomElement[] = [];
  let pos = 132;
  while (pos + 6 <= bytes.byteLength) {
    const group = view.getUint16(pos, true);
    const element = view.getUint16(pos + 2, true);
    const tag = `${group.toString(16).padStart(4, "0")}${element.toString(16).padStart(4, "0")}`.toUpperCase();
    const vr = String.fromCharCode(bytes[pos + 4], bytes[pos + 5]);
    let length: number;
    let valueOffset: number;
    if (_longLengthVrs.has(vr)) {
      if (pos + 12 > bytes.byteLength) break;
      length = view.getUint32(pos + 8, true);
      valueOffset = pos + 12;
    } else {
      if (pos + 8 > bytes.byteLength) break;
      length = view.getUint16(pos + 6, true);
      valueOffset = pos + 8;
    }
    // Length 0xFFFFFFFF marks an undefined-length sequence; treat as end of useful
    // metadata for this minimal viewer and stop walking.
    if (length === 0xffffffff) break;
    elements.push({ tag, vr, length, valueOffset });
    pos = valueOffset + length;
  }
  return elements;
}

/** Decode an element's bytes into a display string. Pixel data and other large opaque
 * blobs get a "N bytes" placeholder so we don't dump megabytes into the DOM. */
function _decodeValue(bytes: Uint8Array, element: DicomElement): string {
  if (element.tag === "7FE00010") return `${element.length} bytes (download to view)`;
  if (element.length === 0) return "";
  const slice = bytes.subarray(element.valueOffset, element.valueOffset + element.length);
  // String-valued VRs (per PS3.5) decode as ASCII; preserve the standard's trailing
  // padding semantics by trimming nulls and trailing spaces.
  switch (element.vr) {
    case "AE":
    case "AS":
    case "CS":
    case "DA":
    case "DS":
    case "DT":
    case "IS":
    case "LO":
    case "LT":
    case "PN":
    case "SH":
    case "ST":
    case "TM":
    case "UI":
    case "UR":
    case "UT": {
      let text = new TextDecoder("utf-8", { fatal: false }).decode(slice);
      text = text.replace(/\0+$/, "").replace(/\s+$/, "");
      return text;
    }
    case "US":
      return String(new DataView(slice.buffer, slice.byteOffset, slice.byteLength).getUint16(0, true));
    case "UL":
      return String(new DataView(slice.buffer, slice.byteOffset, slice.byteLength).getUint32(0, true));
    case "SS":
      return String(new DataView(slice.buffer, slice.byteOffset, slice.byteLength).getInt16(0, true));
    case "SL":
      return String(new DataView(slice.buffer, slice.byteOffset, slice.byteLength).getInt32(0, true));
    default:
      return `${slice.byteLength} bytes`;
  }
}

function _dicomViewer(bytes: Uint8Array, attachmentId: string, mimeType: string): HTMLElement {
  const elements = _parseDicom(bytes);
  if (elements === null) {
    return el("div", { class: "viewer viewer-fallback" }, [
      el("p", { class: "muted" }, `Not a recognisable DICOM file (no DICM preamble at offset 128).`),
      el("a", { href: downloadAttachmentUrl(attachmentId), target: "_blank", rel: "noopener" }, "Download to inspect"),
    ]);
  }

  const tbody = el("tbody");
  let renderedKnown = 0;
  for (const e of elements) {
    const keyword = _wellKnownTags[e.tag];
    if (!keyword) continue;
    renderedKnown++;
    const value = _decodeValue(bytes, e);
    tbody.appendChild(el("tr", {}, [
      el("td", {}, keyword),
      el("td", {}, el("code", {}, `(${e.tag.slice(0, 4)},${e.tag.slice(4)})`)),
      el("td", {}, e.vr),
      el("td", {}, value),
    ]));
  }

  const summary = el("p", { class: "muted" }, [
    `Parsed ${elements.length} elements; showing ${renderedKnown} well-known tags. `,
    el("a", { href: downloadAttachmentUrl(attachmentId), target: "_blank", rel: "noopener" }, "Download for full PACS viewing"),
  ]);

  const children: ElChild[] = [summary];
  if (renderedKnown > 0) {
    children.push(el("table", { class: "viewer viewer-dicom-tags" }, [
      el("thead", {}, el("tr", {}, [
        el("th", {}, "Tag"),
        el("th", {}, "(group,element)"),
        el("th", {}, "VR"),
        el("th", {}, "Value"),
      ])),
      tbody,
    ]));
  } else {
    children.push(el("p", { class: "muted" }, [
      "No recognisable header tags in this file — likely a fragment or a private SOP class. ",
      el("a", { href: downloadAttachmentUrl(attachmentId), target: "_blank", rel: "noopener" }, "Download to inspect"),
    ]));
  }
  return el("div", { class: "viewer viewer-dicom" }, children);
}

// Order-insensitive: slice I's registry sorts longest-prefix-first.
registerViewer("application/dicom", _dicomViewer);
registerViewer("image/dicom", _dicomViewer); // a few partner gateways emit this alias.
