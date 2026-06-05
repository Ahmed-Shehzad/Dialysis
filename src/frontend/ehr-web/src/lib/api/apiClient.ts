import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios";

const baseURL = import.meta.env.VITE_API_BASE_URL ?? "";

export const apiClient = axios.create({
  baseURL,
  withCredentials: true,
  headers: { Accept: "application/json" },
});

type TokenProvider = () => string | null | Promise<string | null>;

let tokenProvider: TokenProvider | null = null;
let onUnauthorized: (() => void) | null = null;

export const configureApiClient = (options: {
  tokenProvider?: TokenProvider;
  onUnauthorized?: () => void;
}) => {
  tokenProvider = options.tokenProvider ?? null;
  onUnauthorized = options.onUnauthorized ?? null;
};

apiClient.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
  if (tokenProvider) {
    const token = await tokenProvider();
    if (token) {
      config.headers.set("Authorization", `Bearer ${token}`);
    } else if ((config.url ?? "").includes("/api/")) {
      // Surface the most common cause of 401s loudly: a gateway-routed call going out
      // without a Bearer. Either the BFF didn't return an accessToken, or it expired
      // (Keycloak access tokens live ~5min by default and there is no refresh flow yet).
      console.warn(
        "[apiClient] " +
          (config.method ?? "GET").toUpperCase() +
          " " +
          (config.url ?? "") +
          " — no access token in tokenStore; gateway will return 401.",
      );
    }
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401 && onUnauthorized) {
      onUnauthorized();
    }
    return Promise.reject(error);
  },
);
