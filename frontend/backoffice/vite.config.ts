/// <reference types="vitest/config" />
import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: { alias: { '@': path.resolve(import.meta.dirname, 'src') } },
  server: {
    port: 5173,
    strictPort: true,
    // O browser vê a API no mesmo endereço, por isso o cookie SameSite=Strict funciona
    // como em produção e não é preciso CORS (docs/04-componentes.md).
    proxy: { '/api': process.env.API_URL ?? 'http://localhost:5079' },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/testes/configuracao.ts'],
    css: false,
    coverage: {
      provider: 'v8',
      include: ['src/**/*.{ts,tsx}'],
      // Os componentes de src/components/ui são os do shadcn/ui, só apresentação.
      exclude: ['src/main.tsx', 'src/components/ui/**', 'src/testes/**', 'src/**/*.test.{ts,tsx}'],
      thresholds: { lines: 80, statements: 80, functions: 80, branches: 70 },
    },
  },
})
