import { pedir } from './cliente'

// Tipos das respostas da API (ver docs/04-componentes.md, "Endpoints").

export type Utilizador = { nome: string; email: string }
export type ConfiguracaoTotp = { segredo: string; uri: string; qrCode: string }

export type Bloco = {
  id: number
  titulo: string
  slug: string
  conteudo: string
  ordem: number
  publico: boolean
}
export type DadosBloco = { titulo: string; conteudo: string; publico: boolean }

export type Imagem = { id: number; nomeOriginal: string; tipoMime: string; tamanhoBytes: number; ordem: number; url: string }
export type Projeto = {
  id: number
  nome: string
  slug: string
  descricao: string | null
  repositorioGithub: string | null
  publico: boolean
  imagens: Imagem[]
}
export type DadosProjeto = { nome: string; descricao: string | null; repositorioGithub: string | null }

export type EstadoTimeline = 'atual' | 'desatualizada' | 'indisponivel' | 'repositorioNaoEncontrado' | 'semRepositorio'
export type Commit = { sha: string; mensagem: string; autor: string; data: string; url: string }
export type Timeline = { estado: EstadoTimeline; atualizadoEm: string | null; commits: Commit[] }

const auth = '/api/backoffice/auth'
const blocos = '/api/backoffice/blocos'
const projetos = '/api/backoffice/projetos'

export const api = {
  // Autenticação (login em dois passos: password e depois o código do 2FA).
  csrf: () => pedir<void>('GET', `${auth}/csrf`),
  login: (email: string, password: string) =>
    pedir<{ proximoPasso: 'codigo' | 'configurar-2fa' }>('POST', `${auth}/login`, { email, password }),
  codigo: (codigo: string) => pedir<Utilizador>('POST', `${auth}/2fa`, { codigo }),
  configurar2fa: () => pedir<ConfiguracaoTotp>('POST', `${auth}/2fa/configurar`),
  ativar2fa: (codigo: string) => pedir<{ codigosRecuperacao: string[] }>('POST', `${auth}/2fa/ativar`, { codigo }),
  logout: () => pedir<void>('POST', `${auth}/logout`),
  eu: () => pedir<Utilizador>('GET', `${auth}/eu`),

  blocos: () => pedir<Bloco[]>('GET', blocos),
  criarBloco: (dados: DadosBloco) => pedir<Bloco>('POST', blocos, dados),
  editarBloco: (id: number, dados: DadosBloco) => pedir<Bloco>('PUT', `${blocos}/${id}`, dados),
  removerBloco: (id: number) => pedir<void>('DELETE', `${blocos}/${id}`),
  reordenarBlocos: (ids: number[]) => pedir<void>('PUT', `${blocos}/ordem`, { ids }),
  preVisualizar: (texto: string) => pedir<{ html: string }>('POST', '/api/backoffice/markdown/pre-visualizar', { texto }),

  projetos: () => pedir<Projeto[]>('GET', projetos),
  projeto: (id: number) => pedir<Projeto>('GET', `${projetos}/${id}`),
  criarProjeto: (dados: DadosProjeto) => pedir<Projeto>('POST', projetos, dados),
  editarProjeto: (id: number, dados: DadosProjeto) => pedir<Projeto>('PUT', `${projetos}/${id}`, dados),
  publicarProjeto: (id: number, publico: boolean) => pedir<Projeto>('PUT', `${projetos}/${id}/publico`, { publico }),
  removerProjeto: (id: number) => pedir<void>('DELETE', `${projetos}/${id}`),
  timeline: (id: number) => pedir<Timeline>('GET', `${projetos}/${id}/timeline`),

  enviarImagem: (projetoId: number, ficheiro: File) => {
    const formulario = new FormData()
    formulario.append('ficheiro', ficheiro)
    return pedir<Imagem>('POST', `${projetos}/${projetoId}/imagens`, formulario)
  },
  reordenarImagens: (projetoId: number, ids: number[]) => pedir<void>('PUT', `${projetos}/${projetoId}/imagens/ordem`, { ids }),
  removerImagem: (id: number) => pedir<void>('DELETE', `/api/backoffice/imagens/${id}`),
}
