import type { Commit } from './api'

// &lt; e &gt; passam a ‹ e ›: o texto das meta tags nunca tem < nem >.
const entidades: Record<string, string> = { '&amp;': '&', '&lt;': '‹', '&gt;': '›', '&quot;': '"', '&#39;': "'", '&nbsp;': ' ' }

// Texto simples (por exemplo a descrição de um projeto): junta os espaços e corta no máximo.
export function resumir(texto: string, maximo = 160): string {
  const junto = texto.replace(/\s+/g, ' ').trim()
  return junto.length <= maximo ? junto : `${junto.slice(0, maximo - 1).trimEnd()}…`
}

// Texto para as meta tags a partir do HTML dos blocos (já sanitizado pela API).
// 1. O fim de um parágrafo, título ou item separa as palavras; as etiquetas dentro do texto
//    (<strong>, <a>…) saem sem deixar espaço, senão "<strong>x</strong>." ficava "x .".
// 2. Tirar etiquetas com uma expressão regular pode deixar restos ("<scr<script>ipt>"), por isso
//    remove-se depois qualquer < ou > que tenha sobrado.
// 3. Só no fim se descodificam as entidades, e nenhuma volta a criar < ou >.
export function textoSimples(html: string, maximo = 160): string {
  const semEtiquetas = html
    .replace(/<\/(p|h[1-6]|li|blockquote|pre)>|<br\s*\/?>/gi, ' ')
    .replace(/<[^>]*>/g, '')
    .replace(/[<>]/g, '')
  return resumir(semEtiquetas.replace(/&(amp|lt|gt|quot|#39|nbsp);/g, (e) => entidades[e] ?? e), maximo)
}

const formatoMes = new Intl.DateTimeFormat('pt-PT', { month: 'long', year: 'numeric', timeZone: 'Europe/Lisbon' })
const formatoDia = new Intl.DateTimeFormat('pt-PT', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'Europe/Lisbon' })

// Commits agrupados por mês ("novembro de 2026"), pela ordem em que vêm (mais recentes primeiro).
export function agruparPorMes(commits: Commit[]): { mes: string; commits: Commit[] }[] {
  const grupos: { mes: string; commits: Commit[] }[] = []
  for (const commit of commits) {
    const mes = formatoMes.format(new Date(commit.data))
    const ultimo = grupos.at(-1)
    if (ultimo?.mes === mes) ultimo.commits.push(commit)
    else grupos.push({ mes, commits: [commit] })
  }
  return grupos
}

export const dataCurta = (data: string) => formatoDia.format(new Date(data))

const relativo = new Intl.RelativeTimeFormat('pt-PT', { numeric: 'auto' })
const unidades: [Intl.RelativeTimeFormatUnit, number][] = [
  ['year', 365 * 24 * 3600],
  ['month', 30 * 24 * 3600],
  ['week', 7 * 24 * 3600],
  ['day', 24 * 3600],
  ['hour', 3600],
  ['minute', 60],
]

// "há 3 dias", "ontem", "há 2 horas".
export function tempoRelativo(data: string, agora: Date = new Date()): string {
  const segundos = (new Date(data).getTime() - agora.getTime()) / 1000
  for (const [unidade, duracao] of unidades) {
    if (Math.abs(segundos) >= duracao) return relativo.format(Math.round(segundos / duracao), unidade)
  }
  return 'agora mesmo'
}
