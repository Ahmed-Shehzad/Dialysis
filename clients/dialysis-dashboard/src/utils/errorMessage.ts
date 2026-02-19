export function getErrorMessage(error: unknown): string | null {
    if (error instanceof Error) return error.message;
    if (error != null) return String(error);
    return null;
}
