import { describe, expect, it, vi } from 'vitest'

// SEG23: cabeçalhos de segurança do site e o rewrite das imagens (next.config.ts).
async function lerConfiguracao(ambiente: 'production' | 'development') {
  vi.resetModules()
  vi.stubEnv('NODE_ENV', ambiente)
  vi.stubEnv('API_URL', 'https://api.interna')
  const config = (await import('../next.config')).default
  const [regra] = await config.headers!()
  const cabecalhos = Object.fromEntries(regra!.headers.map((c) => [c.key, c.value]))
  return { config, regra: regra!, cabecalhos }
}

describe('next.config', () => {
  it('aplica os cabeçalhos de segurança a todas as páginas', async () => {
    const { config, regra, cabecalhos } = await lerConfiguracao('production')

    expect(regra.source).toBe('/:caminho*')
    expect(cabecalhos['X-Content-Type-Options']).toBe('nosniff')
    expect(cabecalhos['X-Frame-Options']).toBe('DENY')
    expect(cabecalhos['Referrer-Policy']).toBe('strict-origin-when-cross-origin')
    expect(cabecalhos['Permissions-Policy']).toContain('geolocation=()')
    expect(config.poweredByHeader).toBe(false)
  })

  it('a CSP só deixa carregar do próprio site e não deixa a página ser embutida', async () => {
    const csp = (await lerConfiguracao('production')).cabecalhos['Content-Security-Policy']

    for (const diretiva of ["default-src 'self'", "img-src 'self' data:", "object-src 'none'", "base-uri 'self'", "form-action 'self'", "frame-ancestors 'none'"])
      expect(csp).toContain(diretiva)
    expect(csp).not.toContain('unsafe-eval')
  })

  it('só em desenvolvimento a CSP permite eval (precisa dele o recarregamento do Next.js)', async () => {
    expect((await lerConfiguracao('development')).cabecalhos['Content-Security-Policy']).toContain("'unsafe-eval'")
  })

  it('as imagens passam pela API configurada, e só as imagens', async () => {
    const { config } = await lerConfiguracao('production')

    expect(await config.rewrites!()).toEqual([
      { source: '/api/publico/imagens/:ficheiro', destination: 'https://api.interna/api/publico/imagens/:ficheiro' },
    ])
  })
})
