import {
    useCallback,
    useLayoutEffect,
    useState,
    type ReactNode,
} from "react";
import { AuthContext } from "./context";
import { setAuthToken } from "./auth-token";

const AUTH_STORAGE_KEY = "dialysis-dashboard-jwt";

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

    const value = {
        token,
        setToken,
        isAuthenticated: Boolean(token?.trim()),
    };

    return (
        <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
    );
}
