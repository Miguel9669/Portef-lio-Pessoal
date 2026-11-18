import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import type { Bloco } from '@/api/api'
import { abrir, falha, ok, sessao, simularApi } from '@/testes/ajudantes'

const bloco = (id: number, titulo: string, publico = true): Bloco => ({ id, titulo, slug: titulo.toLowerCase(), conteudo: `Texto de ${titulo}`, ordem: id, publico })

describe('lista de blocos', () => {
  it('mostra os blocos e oculta ou mostra um bloco no site', async () => {
    let blocos = [bloco(1, 'Entrada'), bloco(2, 'Contactos', false)]
    const chamadas = simularApi({
      ...sessao,
      'GET /api/backoffice/blocos': () => ({ corpo: blocos }),
      'PUT /api/backoffice/blocos/1': (corpo) => {
        blocos = [{ ...blocos[0]!, ...(corpo as object) }, blocos[1]!]
        return { corpo: blocos[0] }
      },
    })
    abrir('/blocos')

    expect(await screen.findByText('Entrada')).toBeInTheDocument()
    expect(screen.getByText('Oculto')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('switch', { name: 'Mostrar Entrada no site' }))

    await waitFor(() => expect(screen.getAllByText('Oculto')).toHaveLength(2))
    expect(chamadas.find((c) => c.metodo === 'PUT')?.corpo).toEqual({ titulo: 'Entrada', conteudo: 'Texto de Entrada', publico: false })
  })

  it('remove um bloco só depois de confirmar', async () => {
    let blocos = [bloco(1, 'Entrada')]
    const chamadas = simularApi({
      ...sessao,
      'GET /api/backoffice/blocos': () => ({ corpo: blocos }),
      'DELETE /api/backoffice/blocos/1': () => {
        blocos = []
        return {}
      },
    })
    abrir('/blocos')

    await userEvent.click(await screen.findByRole('button', { name: 'Remover' }))
    const dialogo = await screen.findByRole('dialog')
    expect(dialogo).toHaveTextContent('Remover "Entrada"?')
    expect(chamadas.some((c) => c.metodo === 'DELETE')).toBe(false)
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Remover' }))

    expect(await screen.findByText('Ainda não há blocos.')).toBeInTheDocument()
  })

  it('mostra o erro da API', async () => {
    simularApi({ ...sessao, 'GET /api/backoffice/blocos': falha(500, 'Erro interno') })
    abrir('/blocos')

    expect(await screen.findByRole('alert')).toHaveTextContent('Erro interno')
  })
})

describe('editor de blocos', () => {
  it('cria um bloco com pré-visualização do HTML sanitizado pela API', async () => {
    const chamadas = simularApi({
      ...sessao,
      'POST /api/backoffice/markdown/pre-visualizar': (corpo) => ({ corpo: { html: `<p><strong>${(corpo as { texto: string }).texto.replaceAll('*', '')}</strong></p>` } }),
      'POST /api/backoffice/blocos': (corpo) => ({ corpo: { ...bloco(3, 'Novo'), ...(corpo as object) } }),
      'GET /api/backoffice/blocos': ok([]),
    })
    abrir('/blocos/novo')

    await userEvent.type(await screen.findByLabelText('Título'), 'Sobre mim')
    await userEvent.type(screen.getByLabelText('Texto (Markdown)'), '**Olá**')
    await userEvent.click(screen.getByRole('switch', { name: 'Visível no site' }))

    const previsualizacao = screen.getByLabelText('Pré-visualização')
    expect(await within(previsualizacao).findByText('Olá', {}, { timeout: 2000 })).toHaveProperty('tagName', 'STRONG')
    await userEvent.click(screen.getByRole('button', { name: 'Gravar' }))

    expect(await screen.findByText('Ainda não há blocos.')).toBeInTheDocument()
    expect(chamadas.find((c) => c.metodo === 'POST' && c.url === '/api/backoffice/blocos')?.corpo).toEqual({ titulo: 'Sobre mim', conteudo: '**Olá**', publico: true })
  })

  it('edita um bloco existente e mostra os erros da API no campo', async () => {
    simularApi({
      ...sessao,
      'GET /api/backoffice/blocos': ok([bloco(1, 'Entrada')]),
      'POST /api/backoffice/markdown/pre-visualizar': ok({ html: '<p>x</p>' }),
      'PUT /api/backoffice/blocos/1': falha(400, 'Validação', { Titulo: ['Título repetido.'] }),
    })
    abrir('/blocos/1')

    expect(await screen.findByDisplayValue('Entrada')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Gravar' }))

    expect(await screen.findByText('Título repetido.')).toBeInTheDocument()
    expect(screen.getByText('Corrija os campos assinalados.')).toBeInTheDocument()
  })

  it('valida o título antes de enviar', async () => {
    simularApi({ ...sessao })
    abrir('/blocos/novo')

    await userEvent.click(await screen.findByRole('button', { name: 'Gravar' }))

    expect(await screen.findByText('Indique o título.')).toBeInTheDocument()
  })

  it('avisa quando o bloco não existe', async () => {
    simularApi({ ...sessao, 'GET /api/backoffice/blocos': ok([]) })
    abrir('/blocos/99')

    expect(await screen.findByText(/Bloco não encontrado/)).toBeInTheDocument()
  })
})
