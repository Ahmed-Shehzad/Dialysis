import {
    useCallback,
    useLayoutEffect,
    useMemo,
    useState,
    type ReactNode,
} from "react";
import { AuthContext } from "./context";
import { setAuthToken } from "./auth-token";

const AUTH_STORAGE_KEY = "dialysis-dashboard-jwt";

export function AuthProvider({ children }: Readonly<{ children: ReactNode }>) {
    const [token, setToken] = useState<string | null>(() => {
        try {
            return sessionStorage.getItem(AUTH_STORAGE_KEY);
        } catch {
            return null;
        }
    });

    const updateToken = useCallback(
        (value: string | null) => {
            setToken(value);
            try {
                if (value) {
                    sessionStorage.setItem(AUTH_STORAGE_KEY, value);
                } else {
                    sessionStorage.removeItem(AUTH_STORAGE_KEY);
                }
            } catch {
                // ignore
            }
        },
        [setToken],
    );

    useLayoutEffect(() => {
        setAuthToken(token);
    }, [token]);

    const value = useMemo(
        () => ({
            token,
            setToken: updateToken,
            isAuthenticated: Boolean(token?.trim()),
        }),
        [token, updateToken],
    );

    return (
        <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
    );
}
