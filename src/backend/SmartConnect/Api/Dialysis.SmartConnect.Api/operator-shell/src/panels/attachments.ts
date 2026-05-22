import {
  deleteAttachment,
  downloadAttachmentUrl,
  fetchAttachmentBytes,
  listAttachmentsForMessage,
} from "../api";
import { el, formatDate, errBlock, clear } from "../dom";
import type { RouteContext } from "../router";
import { pickViewer } from "../viewers";

export async function renderAttachments(ctx: RouteContext): Promise<void> {
  const messageId = ctx.segments[1];
  ctx.target.appendChild(el("h2", {}, "Attachments"));
  if (!messageId) {
    ctx.target.appendChild(el("p", { class: "muted" }, [
      "Open from the message browser — the URL is ",
      el("code", {}, "#attachments/{messageId}"),
      ".",
    ]));
    return;
  }
  ctx.target.appendChild(el("p", { class: "muted" }, ["Message ", el("code", {}, messageId)]));

  const status = el("p", { class: "muted" }, "Loading…");
  ctx.target.appendChild(status);
  try {
    const attachments = await listAttachmentsForMessage(messageId);
    if (attachments.length === 0) {
      status.textContent = "No attachments for this message.";
      return;
    }
    status.remove();

    const previewHost = el("div", { class: "attachment-preview" });

    const tbody = el("tbody");
    for (const a of attachments) {
      const delBtn = el("button", { type: "button" }, "Delete") as HTMLButtonElement;
      const previewBtn = el("button", { type: "button" }, "Preview") as HTMLButtonElement;
      const row = el("tr", {}, [
        el("td", {}, el("code", {}, a.id)),
        el("td", {}, a.mimeType ?? "—"),
        el("td", {}, a.sizeBytes !== undefined ? String(a.sizeBytes) : "—"),
        el("td", {}, formatDate(a.createdUtc)),
        el("td", {}, [
          previewBtn,
          " ",
          el("a", { href: downloadAttachmentUrl(a.id), target: "_blank", rel: "noopener" }, "download"),
          " ",
          delBtn,
        ]),
      ]);
      previewBtn.addEventListener("click", async () => {
        previewBtn.disabled = true;
        try {
          clear(previewHost);
          previewHost.appendChild(el("h3", {}, [
            "Preview ",
            el("code", {}, a.id),
            ` — ${a.mimeType ?? "unknown MIME"}`,
          ]));
          const bytes = await fetchAttachmentBytes(a.id);
          const viewer = pickViewer(a.mimeType);
          previewHost.appendChild(viewer(bytes, a.id, a.mimeType ?? ""));
        } catch (e) {
          previewHost.appendChild(errBlock(`Preview failed: ${(e as Error).message ?? e}`));
        } finally {
          previewBtn.disabled = false;
        }
      });
      delBtn.addEventListener("click", async () => {
        if (!confirm(`Delete attachment ${a.id}?`)) return;
        delBtn.disabled = true;
        try {
          await deleteAttachment(a.id);
          row.remove();
        } catch (e) {
          delBtn.textContent = `failed: ${(e as Error).message ?? e}`;
        }
      });
      tbody.appendChild(row);
    }
    ctx.target.appendChild(el("table", {}, [
      el("thead", {}, el("tr", {}, [
        el("th", {}, "Id"), el("th", {}, "MIME"), el("th", {}, "Size"), el("th", {}, "Created"), el("th", {}, "Actions"),
      ])),
      tbody,
    ]));
    ctx.target.appendChild(previewHost);
  } catch (e) {
    status.remove();
    ctx.target.appendChild(errBlock(`Could not load attachments: ${(e as Error).message ?? e}`));
  }
}
