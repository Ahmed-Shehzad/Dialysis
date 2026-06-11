import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "node:path";

// SmartConnect context app. Served under the gateway's `/smartconnect/*`.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const gateway = env.VITE_GATEWAY_URL ?? "http://localhost:9090";

  return {
    base: "/smartconnect/",
    plugins: [react(), tailwindcss()],
    resolve: { alias: { "@": path.resolve(__dirname, "src") } },
    server: {
      port: 5334,
      host: "0.0.0.0",
      proxy: {
        "/smartconnect/api": { target: gateway, changeOrigin: true, secure: false },
        "/smartconnect/hubs": { target: gateway, changeOrigin: true, ws: true, secure: false },
        "/smartconnect/identity": { target: gateway, changeOrigin: true, secure: false },
      },
    },
  };
});
