import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import type { Projeto, Timeline } from '@/api/api'
import { abrir, falha, ok, sessao, simularApi } from '@/testes/ajudantes'

const base = '/api/backoffice/projetos'
const projeto = (dados: Partial<Projeto> = {}): Projeto => ({
  id: 7,
  nome: 'Portfólio',
  slug: 'portfolio',
  descricao: null,
  repositorioGithub: 'https://github.com/dono/repo',
  publico: false,
  imagens: [],
  ...dados,
})
const timelineVazia: Timeline = { estado: 'semRepositorio', atualizadoEm: null, commits: [] }

describe('lista de projetos', () => {
  it('mostra os projetos com o estado público ou não', async () => {
    simularApi({ ...sessao, [`GET ${base}`]: ok([projeto({ publico: true }), projeto({ id: 8, nome: 'Rascunho', slug: 'rascunho' })]) })
    abrir('/projetos')

    expect(await screen.findByText('Portfólio')).toBeInTheDocument()
    expect(screen.getByText('Público')).toBeInTheDocument()
    expect(screen.getByText('Não público')).toBeInTheDocument()
  })
})

describe('criar e editar projetos', () => {
  it('recusa um link que não seja do GitHub antes de enviar', async () => {
    const chamadas = simularApi({ ...sessao })
    abrir('/projetos/novo')

    await userEvent.type(await screen.findByLabelText('Nome'), 'X')
    await userEvent.type(screen.getByLabelText('Repositório no GitHub (opcional)'), 'https://gitlab.com/dono/repo')
    await userEvent.click(screen.getByRole('button', { name: 'Criar projeto' }))

    expect(await screen.findByText(/Tem de ser o link de um repositório do GitHub/)).toBeInTheDocument()
    expect(chamadas.some((c) => c.metodo === 'POST')).toBe(false)
  })

  it('cria um projeto e abre a página dele', async () => {
    const chamadas = simularApi({
      ...sessao,
      [`POST ${base}`]: ok(projeto()),
      [`GET ${base}`]: ok([]),
      [`GET ${base}/7`]: ok(projeto()),
      [`GET ${base}/7/timeline`]: ok(timelineVazia),
    })
    abrir('/projetos/novo')

    await userEvent.type(await screen.findByLabelText('Nome'), 'Portfólio')
    await userEvent.type(screen.getByLabelText('Repositório no GitHub (opcional)'), 'github.com/dono/repo')
    await userEvent.click(screen.getByRole('button', { name: 'Criar projeto' }))

    expect(await screen.findByText('Endereço no site: /projetos/portfolio')).toBeInTheDocument()
    expect(chamadas.find((c) => c.metodo === 'POST')?.corpo).toEqual({ nome: 'Portfólio', descricao: null, repositorioGithub: 'github.com/dono/repo' })
  })

  it('mostra no campo o erro devolvido pela API', async () => {
    simularApi({ ...sessao, [`POST ${base}`]: falha(400, 'Validação', { RepositorioGithub: ['Repositório recusado pela API.'] }) })
    abrir('/projetos/novo')

    await userEvent.type(await screen.findByLabelText('Nome'), 'X')
    await userEvent.click(screen.getByRole('button', { name: 'Criar projeto' }))

    expect(await screen.findByText('Repositório recusado pela API.')).toBeInTheDocument()
  })

  it('edita, publica e remove um projeto', async () => {
    let atual = projeto({ descricao: 'Antiga' })
    const chamadas = simularApi({
      ...sessao,
      [`GET ${base}/7`]: () => ({ corpo: atual }),
      [`GET ${base}/7/timeline`]: ok(timelineVazia),
      [`GET ${base}`]: ok([]),
      [`PUT ${base}/7`]: (corpo) => ({ corpo: (atual = { ...atual, ...(corpo as object) }) }),
      [`PUT ${base}/7/publico`]: (corpo) => ({ corpo: (atual = { ...atual, ...(corpo as object) }) }),
      [`DELETE ${base}/7`]: ok(),
    })
    abrir('/projetos/7')

    const descricao = await screen.findByLabelText('Descrição')
    expect(descricao).toHaveValue('Antiga')
    await userEvent.clear(descricao)
    await userEvent.click(screen.getByRole('button', { name: 'Gravar' }))
    await waitFor(() => expect(chamadas.find((c) => c.metodo === 'PUT' && c.url === `${base}/7`)?.corpo).toEqual({ nome: 'Portfólio', descricao: null, repositorioGithub: 'https://github.com/dono/repo' }))

    await userEvent.click(screen.getByRole('switch', { name: 'Público no site' }))
    expect(await screen.findByText('Público no site')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Remover projeto' }))
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Remover' }))
    expect(await screen.findByText('Ainda não há projetos.')).toBeInTheDocument()
  })

  it('avisa quando o projeto não existe', async () => {
    simularApi({ ...sessao })
    abrir('/projetos/99')

    expect(await screen.findByRole('alert')).toHaveTextContent('Não encontrado.')
  })
})

describe('imagens do projeto', () => {
  it('envia, mostra e remove imagens', async () => {
    let atual = projeto()
    const imagem = { id: 3, nomeOriginal: 'capa.png', tipoMime: 'image/png', tamanhoBytes: 10, ordem: 0, url: '/api/backoffice/imagens/3' }
    const chamadas = simularApi({
      ...sessao,
      [`GET ${base}/7`]: () => ({ corpo: atual }),
      [`GET ${base}/7/timeline`]: ok(timelineVazia),
      [`POST ${base}/7/imagens`]: () => {
        atual = { ...atual, imagens: [imagem] }
        return { estado: 201, corpo: imagem }
      },
      'DELETE /api/backoffice/imagens/3': () => {
        atual = { ...atual, imagens: [] }
        return {}
      },
    })
    abrir('/projetos/7')

    const ficheiro = new File([new Uint8Array([1, 2, 3])], 'capa.png', { type: 'image/png' })
    await userEvent.upload(await screen.findByLabelText('Adicionar imagens'), ficheiro)

    expect(await screen.findByAltText('capa.png')).toHaveAttribute('src', '/api/backoffice/imagens/3')
    const envio = chamadas.find((c) => c.url === `${base}/7/imagens`)
    expect((envio?.corpo as FormData).get('ficheiro')).toBe(ficheiro)

    await userEvent.click(screen.getByRole('button', { name: 'Remover' }))
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Remover' }))
    await waitFor(() => expect(screen.queryByAltText('capa.png')).not.toBeInTheDocument())
  })

  it('mostra o erro de uma imagem recusada', async () => {
    simularApi({
      ...sessao,
      [`GET ${base}/7`]: ok(projeto()),
      [`GET ${base}/7/timeline`]: ok(timelineVazia),
      [`POST ${base}/7/imagens`]: falha(400, 'Validação', { ficheiro: ['Só são aceites imagens JPEG, PNG ou WebP.'] }),
    })
    abrir('/projetos/7')

    await userEvent.upload(await screen.findByLabelText('Adicionar imagens'), new File(['x'], 'x.png', { type: 'image/png' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Corrija os campos assinalados.')
  })
})

describe('timeline', () => {
  it('mostra as mensagens dos commits como texto, mesmo que tenham HTML (SEG29)', async () => {
    const malicioso = '<img src=x onerror=alert(1)> Corrige o login'
    simularApi({
      ...sessao,
      [`GET ${base}/7`]: ok(projeto()),
      [`GET ${base}/7/timeline`]: ok({
        estado: 'atual',
        atualizadoEm: '2026-11-18T10:00:00Z',
        commits: [
          { sha: 'a'.repeat(40), mensagem: malicioso, autor: 'Ana', data: '2026-11-18T10:00:00Z', url: `https://github.com/dono/repo/commit/${'a'.repeat(40)}` },
          { sha: 'b'.repeat(40), mensagem: 'Link estranho', autor: 'Ana', data: '2026-11-17T10:00:00Z', url: 'javascript:alert(1)' },
        ],
      } satisfies Timeline),
    })
    abrir('/projetos/7')

    const link = await screen.findByRole('link', { name: malicioso })
    expect(link).toHaveAttribute('href', `https://github.com/dono/repo/commit/${'a'.repeat(40)}`)
    expect(link).toHaveAttribute('rel', 'noopener noreferrer')
    expect(link.querySelector('img')).toBeNull()
    expect(document.querySelector('img[src="x"]')).toBeNull()
    // Um link que não seja do github.com nem chega a ser um link.
    expect(screen.queryByRole('link', { name: 'Link estranho' })).toBeNull()
    expect(screen.getByText('Link estranho')).toBeInTheDocument()
  })

  it.each([
    ['desatualizada', 'O GitHub não respondeu'],
    ['indisponivel', 'O GitHub não está a responder'],
    ['repositorioNaoEncontrado', 'O repositório não foi encontrado'],
    ['semRepositorio', 'Indique o repositório do GitHub'],
  ] as const)('explica o estado %s', async (estado, texto) => {
    simularApi({ ...sessao, [`GET ${base}/7`]: ok(projeto()), [`GET ${base}/7/timeline`]: ok({ estado, atualizadoEm: null, commits: [] }) })
    abrir('/projetos/7')

    expect(await screen.findByText(new RegExp(texto))).toBeInTheDocument()
  })
})
