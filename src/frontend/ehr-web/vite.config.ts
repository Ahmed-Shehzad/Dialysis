import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "node:path";

// EHR context app. Served under the gateway's `/ehr/*`; Vite base and proxy are scoped to `/ehr`.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const gateway = env.VITE_GATEWAY_URL ?? "http://localhost:9090";

  return {
    base: "/ehr/",
    plugins: [react(), tailwindcss()],
    resolve: { alias: { "@": path.resolve(__dirname, "src") } },
    server: {
      port: 5332,
      host: "0.0.0.0",
      proxy: {
        "/ehr/api": { target: gateway, changeOrigin: true, secure: false },
        "/ehr/hubs": { target: gateway, changeOrigin: true, ws: true, secure: false },
        "/ehr/identity": { target: gateway, changeOrigin: true, secure: false },
      },
    },
  };
});
