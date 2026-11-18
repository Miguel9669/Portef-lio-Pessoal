// Pedidos à API. O cookie de sessão vai sozinho (HttpOnly, SEG10); o token CSRF é lido do
// cookie XSRF-TOKEN e enviado no cabeçalho X-XSRF-TOKEN em todos os pedidos de escrita (SEG11).

export class ErroApi extends Error {
  readonly estado: number
  // Erros por campo do formulário, com o nome do campo como no frontend (camelCase).
  readonly erros: Record<string, string[]>

  constructor(estado: number, mensagem: string, erros: Record<string, string[]> = {}) {
    super(mensagem)
    this.name = 'ErroApi'
    this.estado = estado
    this.erros = erros
  }
}

export function lerCookie(nome: string): string | undefined {
  const cookie = document.cookie.split('; ').find((c) => c.startsWith(`${nome}=`))
  return cookie === undefined ? undefined : decodeURIComponent(cookie.slice(nome.length + 1))
}

export async function pedir<T>(metodo: 'GET' | 'POST' | 'PUT' | 'DELETE', url: string, corpo?: unknown): Promise<T> {
  const cabecalhos: Record<string, string> = { Accept: 'application/json' }
  if (metodo !== 'GET') {
    const token = lerCookie('XSRF-TOKEN')
    if (token) cabecalhos['X-XSRF-TOKEN'] = token
  }

  let body: BodyInit | undefined
  if (corpo instanceof FormData) {
    body = corpo
  } else if (corpo !== undefined) {
    cabecalhos['Content-Type'] = 'application/json'
    body = JSON.stringify(corpo)
  }

  const resposta = await fetch(url, { method: metodo, headers: cabecalhos, body, credentials: 'same-origin' })
  if (!resposta.ok) throw await erroDe(resposta)
  if (resposta.status === 204) return undefined as T
  return (await resposta.json()) as T
}

const mensagensPorEstado: Record<number, string> = {
  401: 'A sessão terminou. Entre de novo.',
  403: 'Sem permissão para esta ação.',
  404: 'Não encontrado.',
  413: 'O ficheiro é demasiado grande.',
  429: 'Demasiados pedidos. Tente daqui a pouco.',
}

// Respostas de erro em ProblemDetails (SEG26): { title, errors: { Campo: [mensagens] } }.
async function erroDe(resposta: Response): Promise<ErroApi> {
  const predefinida = mensagensPorEstado[resposta.status] ?? 'Ocorreu um erro. Tente de novo.'
  try {
    const problema = (await resposta.json()) as { title?: string; errors?: Record<string, string[]> }
    const erros = Object.fromEntries(
      Object.entries(problema.errors ?? {}).map(([campo, mensagens]) => [campo.charAt(0).toLowerCase() + campo.slice(1), mensagens]),
    )
    const mensagem = Object.keys(erros).length > 0 ? 'Corrija os campos assinalados.' : (problema.title ?? predefinida)
    return new ErroApi(resposta.status, mensagem, erros)
  } catch {
    return new ErroApi(resposta.status, predefinida)
  }
}
