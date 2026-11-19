import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { blocos, projeto, timeline } from '@/testes/dados'

const falsos = vi.hoisted(() => ({
  blocos: vi.fn(),
  projetos: vi.fn(),
  projeto: vi.fn(),
  timeline: vi.fn(),
}))

vi.mock('@/lib/api', async (original) => ({ ...(await original<typeof import('@/lib/api')>()), api: falsos }))
vi.mock('next/navigation', () => ({
  notFound: () => {
    throw new Error('NEXT_NOT_FOUND')
  },
}))

const { ApiIndisponivel } = await import('@/lib/api')
const Inicio = (await import('./page')).default
const { generateMetadata: metadadosInicio } = await import('./page')
const PaginaProjeto = (await import('./projetos/[slug]/page')).default
const { generateMetadata: metadadosProjeto } = await import('./projetos/[slug]/page')
const sitemap = (await import('./sitemap')).default
const robots = (await import('./robots')).default
const NaoEncontrado = (await import('./not-found')).default
const Erro = (await import('./error')).default

const params = (slug: string) => ({ params: Promise.resolve({ slug }) })

beforeEach(() => {
  falsos.blocos.mockResolvedValue(blocos)
  falsos.projetos.mockResolvedValue([projeto(), projeto({ nome: 'Outro', slug: 'outro', imagens: [] })])
  falsos.projeto.mockResolvedValue(projeto())
  falsos.timeline.mockResolvedValue(timeline())
})

describe('página inicial', () => {
  it('mostra os blocos pela ordem e os projetos públicos', async () => {
    render(await Inicio())

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Olá, sou o Miguel')
    expect(screen.getAllByRole('heading', { level: 2 }).map((h) => h.textContent)).toEqual(['01Sobre mim', '02Contactos', '03Projetos'])
    expect(screen.getByRole('link', { name: /Portfólio Pessoal/ })).toHaveAttribute('href', '/projetos/portfolio-pessoal')
  })

  it('com a API em baixo, mostra avisos em vez de ir abaixo', async () => {
    falsos.blocos.mockRejectedValue(new ApiIndisponivel('x'))
    falsos.projetos.mockRejectedValue(new ApiIndisponivel('x'))

    render(await Inicio())

    expect(screen.getAllByRole('status').map((s) => s.textContent)).toEqual([
      'O conteúdo não está disponível neste momento. Volte daqui a pouco.',
      'Os projetos não estão disponíveis neste momento. Volte daqui a pouco.',
    ])
  })

  it('sem projetos, diz que vêm em breve', async () => {
    falsos.projetos.mockResolvedValue([])

    render(await Inicio())

    expect(screen.getByText('Em breve.')).toBeInTheDocument()
  })

  it('as meta tags usam o primeiro bloco', async () => {
    const metadados = await metadadosInicio()

    expect(metadados.title).toEqual({ absolute: 'Portfólio — Olá, sou o Miguel' })
    expect(metadados.description).toBe('Desenvolvedor full-stack.')

    falsos.blocos.mockRejectedValue(new ApiIndisponivel('x'))
    expect((await metadadosInicio()).title).toEqual({ absolute: 'Portfólio' })
  })
})

describe('página do projeto', () => {
  it('mostra o projeto, a galeria e a timeline', async () => {
    render(await PaginaProjeto(params('portfolio-pessoal')))

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Portfólio Pessoal')
    expect(screen.getByRole('link', { name: /Ver código no GitHub/ })).toHaveAttribute('href', 'https://github.com/dono/repo')
    expect(screen.getAllByRole('img')).toHaveLength(2)
    expect(screen.getByRole('link', { name: 'Fase 7: site público' })).toBeInTheDocument()
    expect(falsos.projeto).toHaveBeenCalledWith('portfolio-pessoal')
  })

  it('um projeto que não existe ou não é público dá 404 (SEG16)', async () => {
    falsos.projeto.mockResolvedValue(null)

    await expect(PaginaProjeto(params('privado'))).rejects.toThrow('NEXT_NOT_FOUND')
  })

  it('sem repositório nem timeline, mostra só o resto', async () => {
    falsos.projeto.mockResolvedValue(projeto({ repositorioGithub: null, descricao: null }))
    falsos.timeline.mockRejectedValue(new ApiIndisponivel('x'))
    falsos.blocos.mockRejectedValue(new ApiIndisponivel('x'))

    render(await PaginaProjeto(params('portfolio-pessoal')))

    expect(screen.queryByRole('link', { name: /Ver código no GitHub/ })).toBeNull()
    expect(screen.getByRole('status')).toHaveTextContent('não está disponível neste momento')
  })

  it('as meta tags têm título, descrição e a primeira imagem para as pré-visualizações', async () => {
    const metadados = await metadadosProjeto(params('portfolio-pessoal'))

    expect(metadados.title).toBe('Portfólio Pessoal')
    expect(metadados.description).toBe('Um portfólio com backoffice e 2FA.')
    expect(metadados.openGraph?.images).toEqual([{ url: projeto().imagens[0]!.url, alt: 'Portfólio Pessoal' }])
    expect(metadados.twitter).toMatchObject({ card: 'summary_large_image' })
  })

  it('as meta tags de um projeto sem imagens ou inexistente', async () => {
    falsos.projeto.mockResolvedValue(projeto({ imagens: [], descricao: null }))
    expect((await metadadosProjeto(params('x'))).twitter).toMatchObject({ card: 'summary' })

    falsos.projeto.mockResolvedValue(null)
    expect((await metadadosProjeto(params('x'))).title).toBe('Projeto')
  })
})

describe('cache das páginas (ISR)', () => {
  it('as duas páginas regeneram ao ritmo da camada de dados', async () => {
    const { REVALIDAR_SEGUNDOS } = await import('@/lib/api')
    expect((await import('./page')).revalidate).toBe(REVALIDAR_SEGUNDOS)
    expect((await import('./projetos/[slug]/page')).revalidate).toBe(REVALIDAR_SEGUNDOS)
  })
})

describe('outras rotas', () => {
  it('o sitemap tem o início e os projetos públicos', async () => {
    expect((await sitemap()).map((e) => e.url)).toEqual([
      'http://localhost:3000/',
      'http://localhost:3000/projetos/portfolio-pessoal',
      'http://localhost:3000/projetos/outro',
    ])

    falsos.projetos.mockRejectedValue(new ApiIndisponivel('x'))
    expect(await sitemap()).toHaveLength(1)
  })

  it('o robots deixa ler tudo, incluindo as imagens das pré-visualizações', () => {
    expect(robots()).toEqual({ rules: { userAgent: '*', allow: '/' }, sitemap: 'http://localhost:3000/sitemap.xml' })
  })

  it('a página 404 volta ao início', () => {
    render(<NaoEncontrado />)

    expect(screen.getByRole('link', { name: /Voltar ao início/ })).toHaveAttribute('href', '/')
  })

  it('a página de erro deixa tentar de novo', async () => {
    const tentar = vi.fn()
    render(<Erro reset={tentar} />)

    screen.getByRole('button', { name: 'Tentar de novo' }).click()
    expect(tentar).toHaveBeenCalled()
  })
})
