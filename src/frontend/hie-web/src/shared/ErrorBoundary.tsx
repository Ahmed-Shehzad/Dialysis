import { Component, type ErrorInfo, type ReactNode } from "react";

interface ErrorBoundaryProps {
  children: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
}

/**
 * Top-level error boundary: catches render-phase errors anywhere in the tree and swaps the
 * broken page for a calm full-screen fallback. Same philosophy as `humanizeError` — clinical
 * and operations users never see raw error text or stack traces; the caught error is logged
 * via `console.error` so support diagnostics survive. Wrap it as the outermost layer in
 * `AppProviders` so even provider failures land on the fallback instead of a blank page.
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { hasError: false };

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    // Keep the diagnostics in the console for support — never surface them in the UI.
    console.error("Unhandled application error caught by ErrorBoundary", error, errorInfo);
  }

  render(): ReactNode {
    if (!this.state.hasError) {
      return this.props.children;
    }

    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-950 p-6">
        <div
          role="alert"
          className="w-full max-w-md rounded-xl border border-slate-800 bg-slate-900/70 p-8 text-center shadow-xl"
        >
          <h1 className="text-lg font-semibold text-slate-100">Something went wrong</h1>
          <p className="mt-2 text-sm text-slate-400">
            The page ran into an unexpected problem and couldn't continue. Your data is safe —
            reload the page to pick up where you left off.
          </p>
          <button
            type="button"
            onClick={() => window.location.reload()}
            className="mt-6 rounded-md border border-slate-700 bg-slate-800/40 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-800"
          >
            Reload
          </button>
        </div>
      </div>
    );
  }
}
