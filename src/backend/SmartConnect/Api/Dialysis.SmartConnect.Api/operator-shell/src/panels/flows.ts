import { listFlows } from "../api";
import { el, formatDate } from "../dom";
import type { RouteContext } from "../router";

export async function renderFlows(ctx: RouteContext): Promise<void> {
  void formatDate;
  ctx.target.appendChild(el("h2", {}, "Flows"));
  const status = el("p", { class: "muted" }, "Loading…");
  ctx.target.appendChild(status);
  try {
    const flows = await listFlows();
    if (!Array.isArray(flows) || flows.length === 0) {
      status.textContent = "No flows yet. POST a flow to /api/v1/admin/flows or use import.";
      return;
    }
    status.remove();
    const tbody = el("tbody");
    for (const f of flows) {
      tbody.appendChild(el("tr", {}, [
        el("td", {}, f.name || "—"),
        el("td", {}, el("code", {}, f.id)),
        el("td", {}, f.runtimeState ?? "—"),
        el("td", {}, el("a", {
          href: `/api/v1/admin/flows/${encodeURIComponent(f.id)}/statistics`,
          target: "_blank", rel: "noopener",
        }, "stats")),
      ]));
    }
    ctx.target.appendChild(el("table", {}, [
      el("thead", {}, el("tr", {}, [
        el("th", {}, "Name"), el("th", {}, "Id"), el("th", {}, "State"), el("th", {}, "Statistics"),
      ])),
      tbody,
    ]));
  } catch (e) {
    status.textContent = "";
    status.className = "err";
    status.appendChild(document.createTextNode(`Could not load flows: ${(e as Error).message ?? e}`));
  }
}
