import { Component, type ErrorInfo, type ReactNode } from "react";

interface Props {
    children: ReactNode;
}

interface State {
    hasError: boolean;
    error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
    state: State = { hasError: false, error: null };

    static getDerivedStateFromError(error: Error): State {
        return { hasError: true, error };
    }

    componentDidCatch(error: Error, info: ErrorInfo): void {
        console.error("ErrorBoundary caught:", error, info);
    }

    render(): ReactNode {
        if (this.state.hasError && this.state.error) {
            return (
                <div className="p-6 m-4 border border-red-300 rounded-lg bg-red-50 text-red-800">
                    <h2 className="m-0 mb-2 text-lg font-semibold">
                        Something went wrong
                    </h2>
                    <p className="m-0 mb-4 text-sm">
                        {this.state.error.message}
                    </p>
                    <button
                        type="button"
                        onClick={() =>
                            this.setState({ hasError: false, error: null })
                        }
                        className="px-3 py-1.5 rounded bg-red-200 hover:bg-red-300 text-red-900 text-sm"
                    >
                        Try again
                    </button>
                </div>
            );
        }
        return this.props.children;
    }
}
