import { checkHealth } from "../api";
import { el } from "../dom";
import type { RouteContext } from "../router";

export async function renderHealth(ctx: RouteContext): Promise<void> {
  ctx.target.appendChild(el("h2", {}, "Health"));
  ctx.target.appendChild(el("p", {}, [
    el("a", { href: "/health", target: "_blank", rel: "noopener" },
      el("code", {}, "GET /health")),
  ]));
  const out = el("pre", { class: "json-block" }, "(not checked yet)");
  ctx.target.appendChild(el("p", {}, el("button", {
    type: "button",
    onclick: async () => {
      out.textContent = "Checking…";
      try {
        const res = await checkHealth();
        out.textContent = `${res.status} ${res.statusText}\n${await res.text()}`;
      } catch (e) {
        out.textContent = `failed: ${(e as Error).message ?? e}`;
      }
    },
  }, "Check now")));
  ctx.target.appendChild(out);
}
