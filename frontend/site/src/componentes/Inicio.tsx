import Link from 'next/link'
import type { Bloco, Projeto } from '@/lib/api'
import { HtmlDaApi } from './HtmlDaApi'
import { dominio } from './Projeto'
import { SetaCanto, SetaDireita } from './Icones'
import { TituloSecao } from './Estrutura'

// Topo da página: o primeiro bloco, em destaque.
export function Heroi({ bloco }: { bloco?: Bloco }) {
  return (
    <section id={bloco?.slug} className="relative isolate overflow-hidden">
      <div aria-hidden="true" className="fundo-pontos absolute inset-0 -z-10" />
      <div aria-hidden="true" className="absolute -top-32 left-1/2 -z-10 h-96 w-[48rem] -translate-x-1/2 rounded-full bg-gradient-to-r from-violet-400/30 via-fuchsia-300/25 to-amber-200/30 blur-3xl dark:from-violet-700/25 dark:via-fuchsia-700/15 dark:to-amber-600/10" />
      <div className="mx-auto max-w-6xl px-5 pt-24 pb-20 sm:px-8 sm:pt-32 sm:pb-28">
        <p className="mb-6 inline-flex items-center gap-2 rounded-full border border-stone-200 bg-white/70 px-3 py-1 text-xs font-medium text-stone-600 shadow-sm dark:border-stone-800 dark:bg-stone-900/70 dark:text-stone-300">
          <span className="size-1.5 animate-pulse rounded-full bg-emerald-500" />
          Disponível para novos projetos
        </p>
        <h1 className="max-w-4xl bg-gradient-to-br from-stone-950 via-stone-800 to-stone-500 bg-clip-text text-5xl font-semibold tracking-tight text-balance text-transparent sm:text-7xl dark:from-white dark:via-stone-200 dark:to-stone-500">
          {bloco?.titulo ?? 'Portfólio'}
        </h1>
        {bloco && <HtmlDaApi html={bloco.html} className="conteudo mt-8 max-w-2xl text-xl" />}
        <div className="mt-10 flex flex-wrap gap-3">
          <Link href="/#projetos" className="group inline-flex items-center gap-2 rounded-full bg-stone-900 px-6 py-3 text-sm font-medium text-white shadow-lg shadow-stone-900/20 transition hover:bg-stone-800 dark:bg-white dark:text-stone-900 dark:hover:bg-stone-200">
            Ver projetos
            <SetaDireita className="size-4 transition group-hover:translate-x-0.5" />
          </Link>
        </div>
      </div>
    </section>
  )
}

// Os outros blocos, numerados, com o título à esquerda e o texto à direita em ecrãs grandes.
export function SecaoBloco({ bloco, numero }: { bloco: Bloco; numero: number }) {
  return (
    <section aria-labelledby={`titulo-${bloco.slug}`} id={bloco.slug} className="scroll-mt-20 border-t border-stone-200 dark:border-stone-800">
      <div className="mx-auto grid max-w-6xl gap-8 px-5 py-20 sm:px-8 lg:grid-cols-[1fr_2fr] lg:gap-16">
        <div className="lg:sticky lg:top-28 lg:self-start">
          <TituloSecao id={`titulo-${bloco.slug}`} numero={String(numero).padStart(2, '0')} titulo={bloco.titulo} />
        </div>
        <HtmlDaApi html={bloco.html} className="conteudo max-w-2xl" />
      </div>
    </section>
  )
}

export function CartaoProjeto({ projeto }: { projeto: Projeto }) {
  const capa = projeto.imagens[0]
  return (
    <li>
      <Link
        href={`/projetos/${projeto.slug}`}
        className="group flex h-full flex-col overflow-hidden rounded-3xl border border-stone-200 bg-white shadow-sm transition duration-300 hover:-translate-y-1 hover:shadow-xl hover:shadow-stone-900/5 dark:border-stone-800 dark:bg-stone-900"
      >
        <div className="relative aspect-[16/10] overflow-hidden bg-gradient-to-br from-stone-100 to-stone-200 dark:from-stone-800 dark:to-stone-900">
          {capa ? (
            // As imagens vêm já reprocessadas e com nomes únicos da API; não precisam do otimizador do Next.
            // eslint-disable-next-line @next/next/no-img-element
            <img src={capa.url} alt="" loading="lazy" className="size-full object-cover transition duration-500 group-hover:scale-105" />
          ) : (
            <span className="absolute inset-0 grid place-items-center text-6xl font-semibold text-stone-300 dark:text-stone-700">{projeto.nome.charAt(0)}</span>
          )}
        </div>
        <div className="flex flex-1 flex-col gap-2 p-6">
          <h3 className="flex items-start justify-between gap-4 text-xl font-semibold tracking-tight text-stone-900 dark:text-white">
            {projeto.nome}
            <SetaCanto className="mt-1 size-5 shrink-0 text-stone-400 transition group-hover:translate-x-0.5 group-hover:-translate-y-0.5 group-hover:text-violet-600" />
          </h3>
          {projeto.descricao && <p className="line-clamp-3 text-stone-600 dark:text-stone-400">{projeto.descricao}</p>}
          <div className="mt-auto flex flex-wrap gap-x-4 gap-y-1 pt-3 text-xs font-medium tracking-wide uppercase">
            {projeto.linkOficial && <span className="text-stone-500 dark:text-stone-400">{dominio(projeto.linkOficial)}</span>}
            {projeto.temHistorico && <span className="text-violet-700 dark:text-violet-300">Com timeline do GitHub</span>}
          </div>
        </div>
      </Link>
    </li>
  )
}
