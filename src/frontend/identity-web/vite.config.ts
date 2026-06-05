import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

// Admin console (identity-web). Served under the gateway's `/admin/*`.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const gateway = env.VITE_GATEWAY_URL ?? "http://localhost:9090";

  return {
    base: "/admin/",
    plugins: [react()],
    resolve: { alias: { "@": path.resolve(__dirname, "src") } },
    server: {
      port: 5336,
      host: "0.0.0.0",
      proxy: {
        "/admin/api": { target: gateway, changeOrigin: true, secure: false },
        "/admin/hubs": { target: gateway, changeOrigin: true, ws: true, secure: false },
        "/admin/identity": { target: gateway, changeOrigin: true, secure: false },
      },
    },
  };
});
