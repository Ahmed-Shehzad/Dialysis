import { listMessages, reprocessMessage } from "../api";
import { el, formatDate, clear, errBlock } from "../dom";
import type { RouteContext } from "../router";

export async function renderMessages(ctx: RouteContext): Promise<void> {
  ctx.target.appendChild(el("h2", {}, "Message browser & ledger"));
  ctx.target.appendChild(el("p", { class: "muted" }, [
    "Filters: ", el("code", {}, "flowId"), " (guid), ", el("code", {}, "status"), " (numeric MessageLedgerStatus). Max 30 per page.",
  ]));

  const flowInput = el("input", { type: "text", placeholder: "optional guid", size: 40 }) as HTMLInputElement;
  const statusInput = el("input", { type: "number", placeholder: "e.g. 0", size: 4 }) as HTMLInputElement;
  const errBox = el("div", { class: "err" });
  const resultsBox = el("div");

  const load = async () => {
    clear(errBox);
    clear(resultsBox);
    try {
      const flowId = flowInput.value.trim() || undefined;
      const status = statusInput.value.trim() || undefined;
      const opts: { flowId?: string; status?: string } = {};
      if (flowId) opts.flowId = flowId;
      if (status) opts.status = status;
      const data = await listMessages(opts);
      const items = data.items ?? [];
      if (items.length === 0) {
        errBox.appendChild(document.createTextNode("No messages match."));
        return;
      }
      const tbody = el("tbody");
      for (const m of items) {
        const reproBtn = el("button", { type: "button" }, "Reprocess") as HTMLButtonElement;
        reproBtn.addEventListener("click", async () => {
          reproBtn.disabled = true;
          try {
            await reprocessMessage(m.id);
            reproBtn.textContent = "started";
          } catch (e) {
            reproBtn.textContent = `failed: ${(e as Error).message ?? e}`;
          }
        });
        tbody.appendChild(el("tr", {}, [
          el("td", {}, formatDate(m.createdAtUtc)),
          el("td", {}, String(m.status ?? "—")),
          el("td", {}, el("code", {}, m.correlationId ?? "")),
          el("td", {}, m.detail ?? "—"),
          el("td", {}, [
            el("a", { href: `/api/v1/admin/messages/${encodeURIComponent(m.id)}`, target: "_blank", rel: "noopener" }, "raw"),
            " ",
            el("a", { href: `#attachments/${encodeURIComponent(m.id)}` }, "attachments"),
            " ",
            reproBtn,
          ]),
        ]));
      }
      resultsBox.appendChild(el("table", {}, [
        el("thead", {}, el("tr", {}, [
          el("th", {}, "Created"), el("th", {}, "Status"), el("th", {}, "Correlation"), el("th", {}, "Detail"), el("th", {}, "Actions"),
        ])),
        tbody,
      ]));
    } catch (e) {
      errBox.appendChild(errBlock(`Load failed: ${(e as Error).message ?? e}`));
    }
  };

  const loadBtn = el("button", { type: "button", onclick: load }, "Load messages");
  ctx.target.appendChild(el("p", {}, [
    el("label", {}, ["flowId ", flowInput]),
    " ",
    el("label", {}, ["status ", statusInput]),
    " ",
    loadBtn,
  ]));
  ctx.target.appendChild(errBox);
  ctx.target.appendChild(resultsBox);
}
