import { type Page } from "@playwright/test";
import { readFileSync } from "node:fs";
import { join } from "node:path";

/** Minimal shape of the BFF `/hie/identity/user` probe response stubbed in tests. */
export interface MockUser {
  name?: string;
  permissions?: string[];
  claims?: Record<string, unknown>;
}

/** Stubs the BFF auth probe so the SPA renders as an authenticated user with the given permissions. */
export async function mockAuth(page: Page, user: MockUser = {}): Promise<void> {
  await page.route("**/hie/identity/user", (route) =>
    route.fulfill({
      json: {
        name: user.name ?? "demo-staff",
        roles: [],
        permissions: user.permissions ?? [],
        claims: user.claims ?? {},
        accessToken: "test-token",
      },
    }),
  );
}

/** Aborts SignalR negotiate / hub traffic — there is no realtime backend in these mocked e2e runs. */
export async function stubRealtime(page: Page): Promise<void> {
  await page.route("**/hie/hubs/**", (route) => route.abort());
  await page.route("**/hie/events/**", (route) => route.abort());
}

/**
 * Aborts any `/hie/api` call a panel makes that a spec did not explicitly mock, so the SPA renders
 * its friendly per-panel error state (humanizeError) instead of crashing the page. Register this BEFORE
 * the specific routes — Playwright matches the most-recently-registered handler first.
 */
export async function stubApiCatchAll(page: Page): Promise<void> {
  await page.route("**/hie/api/**", (route) => route.abort());
}

/**
 * Navigates to a lazy-loaded workflow route and waits for its anchor heading. The per-context pages
 * are `React.lazy` chunks; on a cold Vite dev server the very first dynamic import for a chunk can
 * lose a race and fail to load. If the heading doesn't appear quickly we reload once — by then Vite
 * has compiled the chunk, so the second navigation renders reliably. Keeps the walkthrough videos
 * deterministic without paying for a production build.
 */
export async function gotoWorkflow(page: Page, path: string, headingName: string): Promise<void> {
  await page.goto(path);
  const heading = page.getByRole("heading", { name: headingName }).first();
  try {
    await heading.waitFor({ state: "visible", timeout: 8_000 });
  } catch {
    await page.reload();
    await heading.waitFor({ state: "visible", timeout: 20_000 });
  }
}

/** Reads a committed sample PDF from this app's e2e/fixtures/ directory (cwd is the app root). */
export function fixturePdf(name: string): Buffer {
  return readFileSync(join(process.cwd(), "e2e", "fixtures", name));
}

type DocumentRoutesOptions = {
  /** Document id used in the list row + detail responses. */
  id: string;
  /** Rows the documents list returns. */
  rows: Record<string, unknown>[];
  /** Builds the document detail; `signed` flips true after a successful POST /sign. */
  detail: (signed: boolean) => Record<string, unknown>;
  /** Sample PDF filename under e2e/fixtures/ served for GET …/binary. */
  pdfFixture: string;
};

/**
 * Installs a single stateful handler covering the whole HIE documents surface the PdfViewerDrawer
 * touches: list, detail, binary (real PDF bytes so pdfjs renders), preview, AcroForm fill, and
 * sign. Signing flips an in-handler flag so the subsequent detail refetch returns the new signature
 * row — letting a walkthrough show the signature history updating live. Register AFTER
 * stubApiCatchAll so it wins for `/documents…` URLs while everything else stays aborted.
 */
export async function installDocumentRoutes(
  page: Page,
  opts: DocumentRoutesOptions,
): Promise<void> {
  const pdfBytes = fixturePdf(opts.pdfFixture);
  let signed = false;

  await page.route("**/hie/api/v1.0/documents**", async (route) => {
    const request = route.request();
    const method = request.method();
    const path = new URL(request.url()).pathname;

    if (method === "GET" && path.endsWith("/documents")) {
      return route.fulfill({ json: { data: opts.rows } });
    }
    if (method === "GET" && path.endsWith("/binary")) {
      return route.fulfill({ contentType: "application/pdf", body: pdfBytes });
    }
    if (method === "GET" && path.endsWith("/preview")) {
      return route.fulfill({ json: { data: { format: "Pdf", mimeType: "application/pdf" } } });
    }
    if (method === "POST" && path.endsWith("/fill")) {
      const body = request.postDataJSON() as { fieldValues?: Record<string, string> } | null;
      const filled = Object.keys(body?.fieldValues ?? {});
      return route.fulfill({
        json: { data: { documentId: opts.id, filledFieldNames: filled, unknownFields: [] } },
      });
    }
    if (method === "POST" && path.endsWith("/sign")) {
      signed = true;
      return route.fulfill({ json: { data: { documentId: opts.id } } });
    }
    if (method === "GET") {
      return route.fulfill({ json: { data: opts.detail(signed) } });
    }
    return route.fulfill({ status: 204, body: "" });
  });
}

export { expect, test } from "@playwright/test";
