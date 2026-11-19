import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiIndisponivel, REVALIDAR_SEGUNDOS, api, enderecoSite, nomeSite, ouIndisponivel } from './api'

function simularFetch(resposta: () => Response | Promise<Response>) {
  const falso = vi.fn<typeof fetch>(async () => resposta())
  vi.stubGlobal('fetch', falso)
  return falso
}

const json = (corpo: unknown, status = 200) => new Response(JSON.stringify(corpo), { status })

afterEach(() => {
  delete process.env.API_URL
  delete process.env.SITE_URL
  delete process.env.SITE_NOME
})

describe('api', () => {
  it('pede à API pública com cache de 60 segundos (ISR)', async () => {
    process.env.API_URL = 'https://api.interna'
    const fetch = simularFetch(() => json([{ titulo: 'Olá', slug: 'ola', html: '<p>x</p>' }]))

    const blocos = await api.blocos()

    expect(blocos).toEqual([{ titulo: 'Olá', slug: 'ola', html: '<p>x</p>' }])
    expect(fetch).toHaveBeenCalledWith('https://api.interna/api/publico/blocos', {
      headers: { Accept: 'application/json' },
      next: { revalidate: REVALIDAR_SEGUNDOS },
    })
  })

  it('codifica o slug no endereço', async () => {
    const fetch = simularFetch(() => json({}))

    await api.projeto('a/../b c')
    await api.timeline('x?y')

    expect(fetch.mock.calls[0]![0]).toBe('https://localhost:7003/api/publico/projetos/a%2F..%2Fb%20c')
    expect(fetch.mock.calls[1]![0]).toBe('https://localhost:7003/api/publico/projetos/x%3Fy/timeline')
  })

  it('um 404 é null, e as listas vazias continuam listas', async () => {
    simularFetch(() => new Response(null, { status: 404 }))

    expect(await api.projeto('nao-existe')).toBeNull()
    expect(await api.blocos()).toEqual([])
    expect(await api.projetos()).toEqual([])
  })

  it('erros da API e falhas de rede dão ApiIndisponivel', async () => {
    simularFetch(() => new Response(null, { status: 500 }))
    await expect(api.projetos()).rejects.toBeInstanceOf(ApiIndisponivel)

    simularFetch(() => Promise.reject(new TypeError('fetch failed')))
    await expect(api.projetos()).rejects.toThrow('A API não respondeu.')
  })
})

describe('ouIndisponivel', () => {
  it('troca a ApiIndisponivel por "indisponivel" e deixa passar os outros erros', async () => {
    expect(await ouIndisponivel(Promise.resolve(1))).toBe(1)
    expect(await ouIndisponivel(Promise.reject(new ApiIndisponivel('x')))).toBe('indisponivel')
    await expect(ouIndisponivel(Promise.reject(new Error('outro')))).rejects.toThrow('outro')
  })
})

describe('configuração do site', () => {
  it('usa as variáveis de ambiente ou os valores de desenvolvimento', () => {
    expect(enderecoSite()).toBe('http://localhost:3000')
    expect(nomeSite()).toBe('Portfólio')
    process.env.SITE_URL = 'https://exemplo.pt'
    process.env.SITE_NOME = 'Miguel'
    expect(enderecoSite()).toBe('https://exemplo.pt')
    expect(nomeSite()).toBe('Miguel')
  })
})
