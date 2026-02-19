import {
    createContext,
    useCallback,
    useContext,
    useLayoutEffect,
    useState,
    type ReactNode,
} from "react";

const AUTH_STORAGE_KEY = "dialysis-dashboard-jwt";

type AuthContextValue = {
    token: string | null;
    setToken: (token: string | null) => void;
    isAuthenticated: boolean;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
    const [token, setTokenState] = useState<string | null>(() => {
        try {
            return sessionStorage.getItem(AUTH_STORAGE_KEY);
        } catch {
            return null;
        }
    });

    const setToken = useCallback((value: string | null) => {
        setTokenState(value);
        try {
            if (value) {
                sessionStorage.setItem(AUTH_STORAGE_KEY, value);
            } else {
                sessionStorage.removeItem(AUTH_STORAGE_KEY);
            }
        } catch {
            // ignore
        }
    }, []);

    useLayoutEffect(() => {
        setAuthToken(token);
    }, [token]);

    const value: AuthContextValue = {
        token,
        setToken,
        isAuthenticated: Boolean(token?.trim()),
    };

    return (
        <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
    );
}

export function useAuth(): AuthContextValue {
    const ctx = useContext(AuthContext);
    if (!ctx) throw new Error("useAuth must be used within AuthProvider");
    return ctx;
}

let currentToken: string | null = null;

export function setAuthToken(token: string | null) {
    currentToken = token;
}

export function getAuthToken(): string | null {
    return currentToken;
}
