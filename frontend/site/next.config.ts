import type { NextConfig } from 'next'

// Endereço da API, só usado no servidor Next.js; nunca chega ao browser.
const api = process.env.API_URL ?? 'https://localhost:7003'
const desenvolvimento = process.env.NODE_ENV !== 'production'

// SEG23. O Next.js precisa de scripts inline para hidratar a página; em desenvolvimento também de eval.
const csp = [
  "default-src 'self'",
  `script-src 'self' 'unsafe-inline'${desenvolvimento ? " 'unsafe-eval'" : ''}`,
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data:",
  "font-src 'self'",
  "connect-src 'self'",
  "object-src 'none'",
  "base-uri 'self'",
  "form-action 'self'",
  "frame-ancestors 'none'",
].join('; ')

const config: NextConfig = {
  poweredByHeader: false,
  // O browser pede as imagens ao site, que as vai buscar à API (só de projetos públicos, SEG16).
  async rewrites() {
    return [{ source: '/api/publico/imagens/:ficheiro', destination: `${api}/api/publico/imagens/:ficheiro` }]
  },
  async headers() {
    return [
      {
        source: '/:caminho*',
        headers: [
          { key: 'Content-Security-Policy', value: csp },
          { key: 'X-Content-Type-Options', value: 'nosniff' },
          { key: 'X-Frame-Options', value: 'DENY' },
          { key: 'Referrer-Policy', value: 'strict-origin-when-cross-origin' },
          { key: 'Permissions-Policy', value: 'camera=(), microphone=(), geolocation=()' },
        ],
      },
    ]
  },
}

export default config
