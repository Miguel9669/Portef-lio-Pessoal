import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { vi } from 'vitest'
import { App } from '@/App'

type RespostaFalsa = { estado?: number; corpo?: unknown }
type Tratador = (corpo: unknown) => RespostaFalsa
export type Chamada = { metodo: string; url: string; corpo: unknown; cabecalhos: Record<string, string> }

// Substitui o fetch por uma API falsa: "MÉTODO /url" -> resposta. Devolve os pedidos feitos.
export function simularApi(rotas: Record<string, Tratador>): Chamada[] {
  const chamadas: Chamada[] = []
  vi.stubGlobal('fetch', vi.fn(async (url: string, init: RequestInit = {}) => {
    const metodo = init.method ?? 'GET'
    const corpo = typeof init.body === 'string' ? JSON.parse(init.body) : init.body
    chamadas.push({ metodo, url, corpo, cabecalhos: (init.headers ?? {}) as Record<string, string> })
    const tratador = rotas[`${metodo} ${url}`]
    const { estado = 200, corpo: resposta } = tratador ? tratador(corpo) : { estado: 404, corpo: { title: 'Não encontrado.' } }
    return new Response(resposta === undefined ? null : JSON.stringify(resposta), {
      status: resposta === undefined && estado === 200 ? 204 : estado,
      headers: { 'Content-Type': 'application/json' },
    })
  }))
  return chamadas
}

export const ok = (corpo?: unknown): Tratador => () => ({ corpo })
export const falha = (estado: number, title?: string, errors?: Record<string, string[]>): Tratador => () => ({ estado, corpo: { title, errors } })

// A aplicação inteira, numa rota, com a sessão completa simulada (a menos que se diga o contrário).
export function abrir(rota: string) {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  render(
    <QueryClientProvider client={cliente}>
      <MemoryRouter initialEntries={[rota]}>
        <App />
      </MemoryRouter>
    </QueryClientProvider>,
  )
  return cliente
}

export const sessao = { 'GET /api/backoffice/auth/eu': ok({ nome: 'Ana', email: 'ana@exemplo.pt' }) }
