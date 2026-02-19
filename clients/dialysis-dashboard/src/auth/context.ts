import { createContext } from "react";

export type AuthContextValue = {
    token: string | null;
    setToken: (token: string | null) => void;
    isAuthenticated: boolean;
};

export const AuthContext = createContext<AuthContextValue | null>(null);
