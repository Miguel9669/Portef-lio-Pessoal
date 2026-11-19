// Pedidos à API pública (/api/publico), feitos no servidor Next.js (server components).
// O browser nunca fala com a API, exceto para as imagens, que passam pelo rewrite do next.config.ts.

export type Bloco = { titulo: string; slug: string; html: string }
export type Imagem = { url: string; nomeOriginal: string }
export type Projeto = {
  nome: string
  slug: string
  descricao: string | null
  // Só vem se o código for público.
  repositorioGithub: string | null
  // Site oficial do projeto (só https://, validado na API e na base de dados, SEG30).
  linkOficial: string | null
  // Se há timeline de commits para mostrar (o código pode ser privado e o histórico público).
  temHistorico: boolean
  criadoEm: string
  imagens: Imagem[]
}
export type EstadoTimeline = 'atual' | 'desatualizada' | 'indisponivel' | 'repositorioNaoEncontrado' | 'semRepositorio'
export type Commit = { sha: string; mensagem: string; autor: string; data: string; url: string }
export type Timeline = { estado: EstadoTimeline; atualizadoEm: string | null; commits: Commit[] }

// ISR: as páginas são regeneradas no máximo a cada 60 segundos (docs/04-componentes.md).
export const REVALIDAR_SEGUNDOS = 60

export class ApiIndisponivel extends Error {
  constructor(mensagem: string, options?: ErrorOptions) {
    super(mensagem, options)
    this.name = 'ApiIndisponivel'
  }
}

const enderecoApi = () => process.env.API_URL ?? 'https://localhost:7003'

// null quando não existe (404); ApiIndisponivel quando a API não responde ou falha.
async function pedir<T>(caminho: string): Promise<T | null> {
  let resposta: Response
  try {
    resposta = await fetch(`${enderecoApi()}/api/publico${caminho}`, {
      headers: { Accept: 'application/json' },
      next: { revalidate: REVALIDAR_SEGUNDOS },
    })
  } catch (erro) {
    throw new ApiIndisponivel('A API não respondeu.', { cause: erro })
  }
  if (resposta.status === 404) return null
  if (!resposta.ok) throw new ApiIndisponivel(`A API respondeu ${resposta.status}.`)
  return (await resposta.json()) as T
}

export const api = {
  blocos: async () => (await pedir<Bloco[]>('/blocos')) ?? [],
  projetos: async () => (await pedir<Projeto[]>('/projetos')) ?? [],
  projeto: (slug: string) => pedir<Projeto>(`/projetos/${encodeURIComponent(slug)}`),
  timeline: (slug: string) => pedir<Timeline>(`/projetos/${encodeURIComponent(slug)}/timeline`),
}

// Para as páginas que preferem mostrar um aviso a ir abaixo quando a API falha.
export async function ouIndisponivel<T>(promessa: Promise<T>): Promise<T | 'indisponivel'> {
  try {
    return await promessa
  } catch (erro) {
    if (erro instanceof ApiIndisponivel) return 'indisponivel'
    throw erro
  }
}

export const enderecoSite = () => process.env.SITE_URL ?? 'http://localhost:3000'
export const nomeSite = () => process.env.SITE_NOME ?? 'Portfólio'
