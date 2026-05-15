export type ElChild = Node | string | number | null | undefined | false;
export type ElAttrs = Record<string, string | number | boolean | EventListener | null | undefined>;

export function el<K extends keyof HTMLElementTagNameMap>(
  tag: K,
  attrs?: ElAttrs,
  children?: ElChild | ElChild[],
): HTMLElementTagNameMap[K] {
  const node = document.createElement(tag);
  if (attrs) {
    for (const [k, v] of Object.entries(attrs)) {
      if (v === null || v === undefined || v === false) continue;
      if (k.startsWith("on") && typeof v === "function") {
        node.addEventListener(k.slice(2).toLowerCase(), v as EventListener);
      } else if (k === "class") {
        node.className = String(v);
      } else if (k === "for") {
        node.setAttribute("for", String(v));
      } else {
        node.setAttribute(k, String(v));
      }
    }
  }
  appendChildren(node, children);
  return node;
}

export function appendChildren(node: Node, children: ElChild | ElChild[] | undefined) {
  if (children === undefined || children === null || children === false) return;
  const list = Array.isArray(children) ? children : [children];
  for (const c of list) {
    if (c === null || c === undefined || c === false) continue;
    // Only real DOM nodes are appended as-is; every other value (strings,
    // numbers, and crucially exception/user text) goes through a text node so
    // it can never be reinterpreted as HTML.
    node.appendChild(c instanceof Node ? c : document.createTextNode(String(c)));
  }
}

export function clear(node: Element): void {
  while (node.firstChild) node.removeChild(node.firstChild);
}

export function formatDate(iso: string | null | undefined): string {
  if (!iso) return "—";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toISOString().replace("T", " ").replace(/\.\d+Z$/, "Z");
}

export function errBlock(msg: string): HTMLElement {
  return el("p", { class: "err" }, msg);
}
