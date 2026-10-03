import { defineConfig } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';

// The two APIs are proxied rather than called cross-origin, so the browser never issues a
// preflight and neither service needs CORS enabled. The container does the same thing with
// nginx, which keeps dev and production on identical URLs.
const proxyTarget = (envValue, fallback) => ({
  target: envValue ?? fallback,
  changeOrigin: true,
  rewrite: (path) => path.replace(/^\/(core|sales)-api/, ''),
});

export default defineConfig(({ mode }) => ({
  plugins: [svelte()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/core-api': proxyTarget(process.env.CORE_API_URL, 'http://localhost:8080'),
      '/sales-api': proxyTarget(process.env.SALES_API_URL, 'http://localhost:8081'),
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: mode !== 'production',
  },
}));
