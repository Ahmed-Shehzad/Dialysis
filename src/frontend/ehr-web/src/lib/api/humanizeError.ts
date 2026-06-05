interface ProblemBody {
  title?: string;
  detail?: string;
}

interface HttpErrorShape {
  response?: { status?: number; data?: ProblemBody };
  message?: string;
}

const GENERIC = "Something went wrong. Your last action may not have completed.";

/**
 * Translate an axios/fetch error into a sentence suitable for a clinical or operations user.
 * The module hosts return a ProblemDetails-shaped body on errors; prefer its `title`/`detail`,
 * fall back to status-class messages, and never expose raw status codes or stack traces.
 */
export const humanizeError = (error: unknown): string => {
  if (!error) return GENERIC;
  const e = error as HttpErrorShape;
  const data = e.response?.data;
  if (data?.title) {
    return data.detail ? `${data.title} — ${data.detail}` : data.title;
  }
  const status = e.response?.status;
  if (status === 401 || status === 403) return "You don't have permission to do that.";
  if (status === 404) return "We couldn't find what you were looking for.";
  if (status === 409) return "Someone else changed this just now — refresh and try again.";
  if (status && status >= 500)
    return "The system is unavailable right now. Please try again in a moment.";
  if (typeof e.message === "string" && e.message.toLowerCase().includes("network")) {
    return "We couldn't reach the server. Check your connection.";
  }
  return GENERIC;
};
