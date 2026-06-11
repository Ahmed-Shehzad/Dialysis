import type { ReactNode } from "react";
import { Link } from "react-router";
import { MODULE_MANIFESTS } from "./registry";
import type { ModuleSlug } from "./types";

/** A pill-shaped shortcut rendered alongside the module header. */
export type QuickAction = {
  /** Visible label on the chip. Keep terse — verb-first ("Walk-in", "Send Bundle"). */
  label: string;
  /** In-app route (React Router `<Link>`, resolved within this app's `/his` basename).
   * Mutually exclusive with `href`. */
  to?: string;
  /** Full-page URL for a cross-context hop to another `/{context}` app (e.g. `/ehr/patients`).
   * Rendered as a plain `<a>` so the browser loads the other SPA. Use instead of `to` when the
   * target lives outside this app's router. */
  href?: string;
  /** Optional sub-text rendered as a tooltip when hovered (use sparingly). */
  hint?: string;
  /** Optional visual variant. `primary` for the most common action; `secondary` (default)
   * for supporting actions. */
  variant?: "primary" | "secondary";
};

type ModuleHeaderProps = {
  /** Slug used to look up the module's `displayName` / `tagline` / `description` from
   * `MODULE_MANIFESTS`. */
  moduleSlug: ModuleSlug;
  /** 0–3 quick-action shortcuts. Three is the soft cap — beyond that the header competes
   * with the data view below. */
  quickActions?: QuickAction[];
  /** Optional content rendered in the top-right corner — e.g. an environment indicator
   * or count badge. Kept on a single line. */
  rightSlot?: ReactNode;
  /** Optional "Where to find what" bulleted tour rendered behind a collapsed `<details>`
   * — power users ignore it, new users discover it. Each item is a one-line orientation
   * cue. */
  tour?: Array<{ title: string; body: string }>;
};

/**
 * Shared "front door" header rendered at the top of every module's landing page. Gives a
 * cold user the module name, tagline, plain-language description, and 2–3 verb-first
 * quick actions in one ~80-px-tall band — so the first 2 seconds of a page tell the user
 * what it's for, without forcing them to read documentation first.
 *
 * Reads tagline + description from `MODULE_MANIFESTS` to keep the copy in one place: any
 * future Cross-Module Home dashboard sources the same fields, and the nav switcher /
 * screen-reader `aria-describedby` consumes them too.
 */
export const ModuleHeader = ({
  moduleSlug,
  quickActions = [],
  rightSlot,
  tour,
}: ModuleHeaderProps) => {
  const manifest = MODULE_MANIFESTS.find((m) => m.slug === moduleSlug);
  if (!manifest) {
    // Unknown slug — render nothing rather than crash the page. The lint rule below catches
    // the typical typo at type-check time; this guard handles the case where a module's been
    // removed but a page still references it.
    return null;
  }

  return (
    <header className="border-b border-slate-800 pb-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex-1 min-w-0">
          <h1 className="text-xl font-semibold text-clinic-50">{manifest.displayName}</h1>
          {manifest.tagline && (
            <p className="mt-0.5 text-xs uppercase tracking-wider text-slate-500">
              {manifest.tagline}
            </p>
          )}
          {manifest.description && (
            <p className="mt-2 max-w-3xl text-sm text-slate-400">{manifest.description}</p>
          )}
        </div>
        {rightSlot && <div className="text-xs text-slate-400">{rightSlot}</div>}
      </div>

      {quickActions.length > 0 && (
        <div className="mt-3 flex flex-wrap items-center gap-2">
          {quickActions.map((action) => {
            const className =
              "rounded-full border px-3 py-1 text-xs transition " +
              (action.variant === "primary"
                ? "border-clinic-500 bg-clinic-600/30 text-clinic-50 hover:bg-clinic-600/50"
                : "border-slate-700 bg-slate-800/40 text-slate-200 hover:bg-slate-800");
            // A cross-context target (another /{context} app) must be a real anchor so the
            // browser loads that SPA — a React Router <Link> would resolve it inside /his.
            return action.href ? (
              <a
                key={action.href + action.label}
                href={action.href}
                title={action.hint}
                className={className}
              >
                {action.label}
              </a>
            ) : (
              <Link
                key={(action.to ?? "") + action.label}
                to={action.to ?? ""}
                title={action.hint}
                className={className}
              >
                {action.label}
              </Link>
            );
          })}
        </div>
      )}

      {tour && tour.length > 0 && (
        <details className="mt-3 max-w-3xl text-xs text-slate-400">
          <summary className="cursor-pointer text-slate-300 hover:text-clinic-100">
            Where to find what
          </summary>
          <ul className="mt-2 space-y-1 pl-4">
            {tour.map((item) => (
              <li key={item.title}>
                <span className="text-slate-200">{item.title}</span>
                <span className="text-slate-500"> — {item.body}</span>
              </li>
            ))}
          </ul>
        </details>
      )}
    </header>
  );
};
