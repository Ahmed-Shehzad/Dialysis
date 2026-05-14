import { clear, el } from "./dom";

export interface RouteContext {
  hash: string;
  segments: string[];
  target: HTMLElement;
}

export type RouteHandler = (ctx: RouteContext) => void | Promise<void>;

export interface RouteSpec {
  name: string;
  match: (segments: string[]) => boolean;
  render: RouteHandler;
}

export class Router {
  private routes: RouteSpec[] = [];
  private fallback?: RouteHandler;

  constructor(private readonly target: HTMLElement) {}

  add(route: RouteSpec): this {
    this.routes.push(route);
    return this;
  }

  setFallback(handler: RouteHandler): this {
    this.fallback = handler;
    return this;
  }

  start(): void {
    window.addEventListener("hashchange", () => this.dispatch());
    this.dispatch();
  }

  private async dispatch(): Promise<void> {
    const raw = window.location.hash.replace(/^#/, "");
    const segments = raw.split("/").filter(Boolean);
    const ctx: RouteContext = { hash: raw, segments, target: this.target };
    clear(this.target);
    const match = this.routes.find(r => r.match(segments));
    try {
      if (match) {
        await match.render(ctx);
      } else if (this.fallback) {
        await this.fallback(ctx);
      } else {
        this.target.appendChild(el("p", { class: "err" }, `No panel for #${raw}.`));
      }
    } catch (e) {
      this.target.appendChild(el("p", { class: "err" }, `Panel error: ${(e as Error).message ?? e}`));
    }
  }
}
