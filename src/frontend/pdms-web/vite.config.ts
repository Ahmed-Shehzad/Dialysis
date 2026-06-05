import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

// PDMS context app. Served under the gateway's `/pdms/*`; base + proxy scoped to `/pdms`.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const gateway = env.VITE_GATEWAY_URL ?? "http://localhost:9090";

  return {
    base: "/pdms/",
    plugins: [react()],
    resolve: { alias: { "@": path.resolve(__dirname, "src") } },
    server: {
      port: 5333,
      host: "0.0.0.0",
      proxy: {
        "/pdms/api": { target: gateway, changeOrigin: true, secure: false },
        "/pdms/hubs": { target: gateway, changeOrigin: true, ws: true, secure: false },
        "/pdms/identity": { target: gateway, changeOrigin: true, secure: false },
      },
    },
  };
});
