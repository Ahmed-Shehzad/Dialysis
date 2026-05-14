import { getPrunerOptions } from "../api";
import { el, errBlock } from "../dom";
import type { RouteContext } from "../router";

export async function renderPruner(ctx: RouteContext): Promise<void> {
  ctx.target.appendChild(el("h2", {}, "Data pruner"));
  ctx.target.appendChild(el("p", { class: "muted" }, [
    "Background sweep configuration. Source: ",
    el("code", {}, "GET /smartconnect/v1/admin/pruner/options"),
    ".",
  ]));
  try {
    const opts = await getPrunerOptions();
    ctx.target.appendChild(el("pre", { class: "json-block" }, JSON.stringify(opts, null, 2)));
  } catch (e) {
    ctx.target.appendChild(errBlock(`Could not load pruner options: ${(e as Error).message ?? e}`));
  }
}
