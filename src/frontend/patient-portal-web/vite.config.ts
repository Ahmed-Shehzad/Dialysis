import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

// Patient portal. Served under the gateway's `/portal/*`.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const gateway = env.VITE_GATEWAY_URL ?? "http://localhost:9090";

  return {
    base: "/portal/",
    plugins: [react()],
    resolve: { alias: { "@": path.resolve(__dirname, "src") } },
    server: {
      port: 5337,
      host: "0.0.0.0",
      proxy: {
        "/portal/api": { target: gateway, changeOrigin: true, secure: false },
        "/portal/hubs": { target: gateway, changeOrigin: true, ws: true, secure: false },
        "/portal/identity": { target: gateway, changeOrigin: true, secure: false },
      },
    },
  };
});
