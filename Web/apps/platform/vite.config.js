import path from "path"
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  // Vitest, AYNI vite.config.js'i okuyor - ayrı bir vitest.config.js
  // AÇILMADI, tek bir dosyada dev/build/test yapılandırması bir arada
  // kalsın diye (alias'ların ikisinde de aynı olması gerekiyor zaten).
  test: {
    environment: "jsdom",
    setupFiles: "./src/test/setup.js",
    globals: true,
  },
})