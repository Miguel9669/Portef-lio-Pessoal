/* eslint-disable @next/next/no-img-element -- As imagens vêm já reprocessadas e com nomes únicos da API. */
import type { EstadoTimeline, Imagem, Timeline } from '@/lib/api'
import { agruparPorMes, dataCurta, tempoRelativo } from '@/lib/texto'
import { Aviso, TituloSecao } from './Estrutura'
import { Github, SetaCanto } from './Icones'

export function Galeria({ imagens, nome }: { imagens: Imagem[]; nome: string }) {
  const [primeira, ...resto] = imagens
  if (!primeira) return null
  return (
    <section aria-label={`Imagens de ${nome}`} className="mx-auto max-w-6xl px-5 sm:px-8">
      <figure className="overflow-hidden rounded-3xl border border-stone-200 bg-stone-100 shadow-2xl shadow-stone-900/10 dark:border-stone-800 dark:bg-stone-900">
        <img src={primeira.url} alt={`${nome}: ${primeira.nomeOriginal}`} className="w-full object-cover" />
      </figure>
      {resto.length > 0 && (
        <ul className="mt-4 grid grid-cols-2 gap-4 sm:grid-cols-3">
          {resto.map((imagem) => (
            <li key={imagem.url} className="overflow-hidden rounded-2xl border border-stone-200 dark:border-stone-800">
              <img src={imagem.url} alt={`${nome}: ${imagem.nomeOriginal}`} loading="lazy" className="aspect-[16/10] w-full object-cover" />
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

const avisos: Partial<Record<EstadoTimeline, string>> = {
  desatualizada: 'O GitHub não está a responder; estes são os commits da última atualização.',
  indisponivel: 'O histórico do GitHub não está disponível neste momento. Volte daqui a pouco.',
  repositorioNaoEncontrado: 'O repositório deste projeto não está disponível publicamente.',
}

// Timeline (RF11, RF12): os commits vêm do GitHub e são sempre mostrados como texto, nunca como
// HTML (SEG29). Só são links os que apontam para o github.com.
export function TimelineCommits({ timeline, agora = new Date() }: { timeline: Timeline | 'indisponivel'; agora?: Date }) {
  const dados = timeline === 'indisponivel' ? { estado: 'indisponivel' as const, commits: [], atualizadoEm: null } : timeline
  if (dados.estado === 'semRepositorio') return null
  const aviso = avisos[dados.estado]

  return (
    <section aria-labelledby="titulo-timeline" className="mx-auto max-w-6xl scroll-mt-20 px-5 py-20 sm:px-8">
      <div className="grid gap-10 lg:grid-cols-[1fr_2fr] lg:gap-16">
        <div className="lg:sticky lg:top-28 lg:self-start">
          <TituloSecao id="titulo-timeline" numero="02" titulo="Histórico" />
          <p className="mt-4 text-stone-600 dark:text-stone-400">
            Os commits mais recentes do repositório, tal como estão no GitHub.
          </p>
        </div>
        <div className="flex flex-col gap-10">
          {aviso && <Aviso>{aviso}</Aviso>}
          {agruparPorMes(dados.commits).map((grupo) => (
            <div key={grupo.mes}>
              <h3 className="mb-4 text-sm font-semibold tracking-wide text-stone-900 capitalize dark:text-white">{grupo.mes}</h3>
              <ol className="relative border-l border-stone-200 dark:border-stone-800">
                {grupo.commits.map((commit) => {
                  const doGithub = commit.url.startsWith('https://github.com/')
                  return (
                    <li key={commit.sha} className="group relative pb-6 pl-6 last:pb-0">
                      <span className="absolute top-2 -left-[5px] size-2.5 rounded-full border-2 border-stone-50 bg-violet-500 ring-4 ring-violet-500/10 dark:border-stone-950" />
                      {doGithub ? (
                        <a href={commit.url} target="_blank" rel="noopener noreferrer" className="font-medium text-stone-900 decoration-violet-400 underline-offset-4 hover:underline dark:text-stone-100">
                          {commit.mensagem}
                        </a>
                      ) : (
                        <span className="font-medium text-stone-900 dark:text-stone-100">{commit.mensagem}</span>
                      )}
                      <p className="mt-1 flex flex-wrap items-center gap-x-2 text-sm text-stone-500">
                        <span>{commit.autor}</span>
                        <span aria-hidden="true">·</span>
                        <time dateTime={commit.data} title={dataCurta(commit.data)}>{tempoRelativo(commit.data, agora)}</time>
                        <span aria-hidden="true">·</span>
                        <code className="font-mono text-xs">{commit.sha.slice(0, 7)}</code>
                      </p>
                    </li>
                  )
                })}
              </ol>
            </div>
          ))}
          {dados.commits.length === 0 && !aviso && <p className="text-stone-500">Ainda não há commits.</p>}
        </div>
      </div>
    </section>
  )
}

// Botões da página do projeto: o site oficial é o principal; o código no GitHub, o secundário.
export function LigacoesProjeto({ linkOficial, repositorioGithub }: { linkOficial: string | null; repositorioGithub: string | null }) {
  if (!linkOficial && !repositorioGithub) return null
  return (
    <div className="mt-8 flex flex-wrap gap-3">
      {linkOficial && <BotaoSiteOficial url={linkOficial} />}
      {repositorioGithub && <BotaoGithub url={repositorioGithub} />}
    </div>
  )
}

// SEG30: abre noutro separador, sem dar ao site do projeto acesso a esta página (noopener) nem
// dizer de onde veio o visitante (noreferrer). O link só é aceite se for https://.
export function BotaoSiteOficial({ url }: { url: string }) {
  if (!url.startsWith('https://')) return null
  return (
    <a
      href={url}
      target="_blank"
      rel="noopener noreferrer"
      className="group inline-flex items-center gap-2 rounded-full bg-stone-900 px-5 py-2.5 text-sm font-medium text-white shadow-lg shadow-stone-900/20 transition hover:bg-stone-800 dark:bg-white dark:text-stone-900 dark:hover:bg-stone-200"
    >
      Visitar site
      <span className="text-stone-400 dark:text-stone-500">{dominio(url)}</span>
      <SetaCanto className="size-4 transition group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
    </a>
  )
}

export function dominio(url: string): string {
  try {
    return new URL(url).hostname.replace(/^www\./, '')
  } catch {
    return ''
  }
}

export function BotaoGithub({ url }: { url: string }) {
  return (
    <a
      href={url}
      target="_blank"
      rel="noopener noreferrer"
      className="group inline-flex items-center gap-2 rounded-full border border-stone-300 bg-white px-5 py-2.5 text-sm font-medium text-stone-900 shadow-sm transition hover:border-stone-400 hover:shadow dark:border-stone-700 dark:bg-stone-900 dark:text-white"
    >
      <Github className="size-4" />
      Ver código no GitHub
      <SetaCanto className="size-4 text-stone-400 transition group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
    </a>
  )
}
