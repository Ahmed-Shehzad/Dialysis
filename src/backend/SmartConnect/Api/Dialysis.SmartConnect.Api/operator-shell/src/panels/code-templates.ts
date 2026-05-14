import { getCodeTemplateLibrary, listCodeTemplateLibraries } from "../api";
import { el, errBlock } from "../dom";
import type { RouteContext } from "../router";

export async function renderCodeTemplates(ctx: RouteContext): Promise<void> {
  const libId = ctx.segments[1];
  ctx.target.appendChild(el("h2", {}, "Code template libraries"));
  ctx.target.appendChild(el("p", { class: "muted" }, [
    "Read-only viewer. Author libraries via ",
    el("code", {}, "POST /code-template-libraries/import-mirth-xml"), " or per-resource CRUD.",
  ]));

  const status = el("p", { class: "muted" }, "Loading…");
  ctx.target.appendChild(status);
  try {
    if (libId) {
      const lib = await getCodeTemplateLibrary(libId);
      status.remove();
      ctx.target.appendChild(el("p", {}, el("a", { href: "#code-templates" }, "← back to libraries")));
      ctx.target.appendChild(renderLibraryDetail(lib));
    } else {
      const libs = await listCodeTemplateLibraries();
      if (libs.length === 0) {
        status.textContent = "No code template libraries.";
        return;
      }
      status.remove();
      const tbody = el("tbody");
      for (const lib of libs) {
        tbody.appendChild(el("tr", {}, [
          el("td", {}, lib.name ?? "—"),
          el("td", {}, el("code", {}, lib.id)),
          el("td", {}, String((lib.templates ?? []).length)),
          el("td", {}, el("a", { href: `#code-templates/${encodeURIComponent(lib.id)}` }, "open")),
        ]));
      }
      ctx.target.appendChild(el("table", {}, [
        el("thead", {}, el("tr", {}, [
          el("th", {}, "Name"), el("th", {}, "Id"), el("th", {}, "Templates"), el("th", {}, ""),
        ])),
        tbody,
      ]));
    }
  } catch (e) {
    status.remove();
    ctx.target.appendChild(errBlock(`Could not load: ${(e as Error).message ?? e}`));
  }
}

function renderLibraryDetail(lib: { id: string; name?: string; description?: string; templates?: { name?: string; body?: string; contextString?: string }[] }): HTMLElement {
  const root = el("div");
  root.appendChild(el("h3", {}, lib.name ?? lib.id));
  if (lib.description) root.appendChild(el("p", { class: "muted" }, lib.description));
  const templates = lib.templates ?? [];
  if (templates.length === 0) {
    root.appendChild(el("p", { class: "muted" }, "No templates in this library."));
    return root;
  }
  for (const t of templates) {
    root.appendChild(el("h4", {}, t.name ?? "(unnamed)"));
    if (t.contextString) root.appendChild(el("p", { class: "muted" }, `Context: ${t.contextString}`));
    root.appendChild(el("pre", { class: "code-block" }, t.body ?? ""));
  }
  return root;
}
