import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { blocos, projeto, timeline } from '@/testes/dados'
import { Aviso, Cabecalho, Rodape, ligacoesDoMenu } from './Estrutura'
import { CartaoProjeto, Heroi, SecaoBloco } from './Inicio'
import { BotaoGithub, Galeria, LigacoesProjeto, TimelineCommits } from './Projeto'

describe('estrutura', () => {
  it('o menu tem os blocos (menos o do topo) e os projetos', () => {
    expect(ligacoesDoMenu(blocos)).toEqual([
      { href: '/#sobre-mim', texto: 'Sobre mim' },
      { href: '/#contactos', texto: 'Contactos' },
      { href: '/#projetos', texto: 'Projetos' },
    ])
  })

  it('o cabeçalho mostra o nome e as ligações', () => {
    render(<Cabecalho nome="miguel" ligacoes={ligacoesDoMenu(blocos)} />)

    expect(screen.getByText('M')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Sobre mim' })).toHaveAttribute('href', '/#sobre-mim')
  })

  it('o rodapé tem o ano e o aviso tem o papel de estado', () => {
    render(<><Rodape nome="Miguel" /><Aviso>Indisponível</Aviso></>)

    expect(screen.getByText(new RegExp(`© ${new Date().getFullYear()} Miguel`))).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('Indisponível')
  })
})

describe('início', () => {
  it('o topo mostra o primeiro bloco com o HTML da API', () => {
    render(<Heroi bloco={blocos[0]} />)

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Olá, sou o Miguel')
    expect(screen.getByText('full-stack').tagName).toBe('STRONG')
  })

  it('sem blocos, o topo mostra o nome genérico', () => {
    render(<Heroi />)

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Portfólio')
  })

  it('cada bloco é uma secção numerada com âncora', () => {
    const { container } = render(<SecaoBloco bloco={blocos[1]!} numero={1} />)

    expect(container.querySelector('section#sobre-mim')).not.toBeNull()
    expect(screen.getByRole('heading', { level: 2 })).toHaveTextContent('01')
  })

  it('o cartão do projeto liga à página e usa a primeira imagem como capa', () => {
    render(<ul><CartaoProjeto projeto={projeto()} /></ul>)

    const link = screen.getByRole('link')
    expect(link).toHaveAttribute('href', '/projetos/portfolio-pessoal')
    expect(link.querySelector('img')).toHaveAttribute('src', projeto().imagens[0]!.url)
    expect(screen.getByText('Com timeline do GitHub')).toBeInTheDocument()
  })

  it('um projeto sem imagens nem repositório mostra a inicial', () => {
    render(<ul><CartaoProjeto projeto={projeto({ imagens: [], repositorioGithub: null, descricao: null, temHistorico: false })} /></ul>)

    expect(screen.getByText('P')).toBeInTheDocument()
    expect(screen.queryByText('Com timeline do GitHub')).toBeNull()
  })
})

describe('página do projeto', () => {
  it('a galeria mostra a primeira imagem em destaque e as outras numa grelha', () => {
    render(<Galeria imagens={projeto().imagens} nome="Portfólio" />)

    expect(screen.getAllByRole('img').map((i) => i.getAttribute('alt'))).toEqual(['Portfólio: inicio.png', 'Portfólio: backoffice.png'])
  })

  it('sem imagens, não há galeria', () => {
    const { container } = render(<Galeria imagens={[]} nome="X" />)

    expect(container).toBeEmptyDOMElement()
  })

  it('o cartão mostra o domínio do site oficial', () => {
    render(<ul><CartaoProjeto projeto={projeto({ linkOficial: 'https://www.meuapp.pt/', temHistorico: false })} /></ul>)

    expect(screen.getByText('meuapp.pt')).toBeInTheDocument()
    expect(screen.queryByText('Com timeline do GitHub')).toBeNull()
  })

  it('o site oficial é o botão principal e o GitHub o secundário, ambos noutro separador (SEG30)', () => {
    render(<LigacoesProjeto linkOficial="https://meuapp.pt/" repositorioGithub="https://github.com/dono/repo" />)

    const [site, codigo] = screen.getAllByRole('link')
    expect(site).toHaveTextContent('Visitar site')
    expect(site).toHaveTextContent('meuapp.pt')
    expect(site).toHaveAttribute('href', 'https://meuapp.pt/')
    expect(site).toHaveAttribute('rel', 'noopener noreferrer')
    expect(site).toHaveAttribute('target', '_blank')
    expect(codigo).toHaveTextContent('Ver código no GitHub')
  })

  it('só com o código privado e sem site oficial, não há botões; um link que não seja https nunca é botão', () => {
    const { container, rerender } = render(<LigacoesProjeto linkOficial={null} repositorioGithub={null} />)
    expect(container).toBeEmptyDOMElement()

    rerender(<LigacoesProjeto linkOficial="javascript:alert(1)" repositorioGithub={null} />)
    expect(screen.queryByRole('link')).toBeNull()
  })

  it('o botão do GitHub abre noutro separador sem dar acesso à página', () => {
    render(<BotaoGithub url="https://github.com/dono/repo" />)

    expect(screen.getByRole('link')).toHaveAttribute('rel', 'noopener noreferrer')
    expect(screen.getByRole('link')).toHaveAttribute('target', '_blank')
  })

  it('a timeline agrupa os commits por mês, com tempo relativo', () => {
    render(<TimelineCommits timeline={timeline()} agora={new Date('2026-11-19T12:00:00Z')} />)

    expect(screen.getByRole('heading', { name: 'novembro de 2026' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'outubro de 2026' })).toBeInTheDocument()
    expect(screen.getByText('há 3 horas')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Fase 7: site público' })).toHaveAttribute('href', `https://github.com/dono/repo/commit/${'a'.repeat(40)}`)
  })

  it('as mensagens dos commits são texto, mesmo com HTML, e só há links para o github.com (SEG29)', () => {
    const malicioso = '<img src=x onerror=alert(1)> Corrige o login'
    render(
      <TimelineCommits
        timeline={timeline({
          commits: [
            { sha: 'c'.repeat(40), mensagem: malicioso, autor: '<b>Eva</b>', data: '2026-11-19T09:00:00Z', url: 'https://github.com/dono/repo/commit/c' },
            { sha: 'd'.repeat(40), mensagem: 'Link estranho', autor: 'Eva', data: '2026-11-19T08:00:00Z', url: 'javascript:alert(1)' },
          ],
        })}
      />,
    )

    expect(screen.getByRole('link', { name: malicioso })).toBeInTheDocument()
    expect(screen.getByText('<b>Eva</b>')).toBeInTheDocument()
    expect(document.querySelector('img')).toBeNull()
    expect(document.querySelector('b')).toBeNull()
    expect(screen.queryByRole('link', { name: 'Link estranho' })).toBeNull()
    expect(screen.getByText('Link estranho')).toBeInTheDocument()
  })

  it.each([
    ['desatualizada', 'O GitHub não está a responder'],
    ['repositorioNaoEncontrado', 'não está disponível publicamente'],
  ] as const)('explica o estado %s', (estado, texto) => {
    render(<TimelineCommits timeline={timeline({ estado, commits: [] })} />)

    expect(screen.getByRole('status')).toHaveTextContent(texto)
  })

  it('sem resposta da API, avisa que o histórico está indisponível', () => {
    render(<TimelineCommits timeline="indisponivel" />)

    expect(screen.getByRole('status')).toHaveTextContent('não está disponível neste momento')
  })

  it('sem repositório, não mostra a secção; sem commits, diz que ainda não há', () => {
    const { container, rerender } = render(<TimelineCommits timeline={timeline({ estado: 'semRepositorio', commits: [] })} />)
    expect(container).toBeEmptyDOMElement()

    rerender(<TimelineCommits timeline={timeline({ commits: [] })} />)
    expect(within(container).getByText('Ainda não há commits.')).toBeInTheDocument()
  })
})
