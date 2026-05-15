import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const gateway = env.VITE_GATEWAY_URL ?? "http://localhost:5000";

  return {
    plugins: [react()],
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "src"),
      },
    },
    server: {
      port: 5173,
      host: "0.0.0.0",
      proxy: {
        "/api": { target: gateway, changeOrigin: true, secure: false },
        "/fhir": { target: gateway, changeOrigin: true, secure: false },
        "/hubs": { target: gateway, changeOrigin: true, ws: true, secure: false },
        "/identity": { target: gateway, changeOrigin: true, secure: false },
        "/auth": { target: gateway, changeOrigin: true, secure: false },
      },
    },
  };
});
