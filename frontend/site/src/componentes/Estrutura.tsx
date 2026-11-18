import Link from 'next/link'
import type { ReactNode } from 'react'
import type { Bloco } from '@/lib/api'

export type Ligacao = { href: string; texto: string }

// Ligações do menu: os blocos (exceto o primeiro, que é o topo da página) e os projetos.
export function ligacoesDoMenu(blocos: Bloco[]): Ligacao[] {
  return [...blocos.slice(1).map((b) => ({ href: `/#${b.slug}`, texto: b.titulo })), { href: '/#projetos', texto: 'Projetos' }]
}

export function Cabecalho({ nome, ligacoes }: { nome: string; ligacoes: Ligacao[] }) {
  return (
    <header className="sticky top-0 z-20 border-b border-stone-200/70 bg-stone-50/80 backdrop-blur-md dark:border-stone-800/70 dark:bg-stone-950/80">
      <nav aria-label="Principal" className="mx-auto flex h-16 max-w-6xl items-center gap-6 px-5 sm:px-8">
        <Link href="/" className="flex items-center gap-2 font-semibold tracking-tight text-stone-900 dark:text-white">
          <span className="grid size-7 place-items-center rounded-lg bg-gradient-to-br from-violet-600 to-fuchsia-500 text-sm text-white shadow-sm shadow-violet-500/30">
            {nome.charAt(0).toUpperCase()}
          </span>
          {nome}
        </Link>
        <ul className="ml-auto hidden items-center gap-1 text-sm sm:flex">
          {ligacoes.map((l) => (
            <li key={l.href}>
              <Link href={l.href} className="rounded-full px-3 py-1.5 text-stone-600 transition hover:bg-stone-200/60 hover:text-stone-900 dark:text-stone-400 dark:hover:bg-stone-800 dark:hover:text-white">
                {l.texto}
              </Link>
            </li>
          ))}
        </ul>
        <Link href="/#projetos" className="ml-auto rounded-full bg-stone-900 px-4 py-1.5 text-sm font-medium text-white sm:hidden dark:bg-white dark:text-stone-900">
          Projetos
        </Link>
      </nav>
    </header>
  )
}

export function Rodape({ nome }: { nome: string }) {
  return (
    <footer className="border-t border-stone-200 dark:border-stone-800">
      <div className="mx-auto flex max-w-6xl flex-col gap-2 px-5 py-10 text-sm text-stone-500 sm:flex-row sm:items-center sm:justify-between sm:px-8">
        <p>© {new Date().getFullYear()} {nome}</p>
        <p>Feito com ASP.NET Core, Next.js e muito café.</p>
      </div>
    </footer>
  )
}

export function Aviso({ children }: { children: ReactNode }) {
  return (
    <p role="status" className="rounded-2xl border border-amber-200 bg-amber-50 px-5 py-4 text-amber-900 dark:border-amber-900/60 dark:bg-amber-950/40 dark:text-amber-200">
      {children}
    </p>
  )
}

export function TituloSecao({ numero, titulo, id }: { numero: string; titulo: string; id?: string }) {
  return (
    <h2 id={id} className="flex items-baseline gap-3 text-sm font-medium tracking-widest text-stone-500 uppercase dark:text-stone-400">
      <span className="font-mono text-violet-600 dark:text-violet-400">{numero}</span>
      <span className="h-px w-8 bg-stone-300 dark:bg-stone-700" />
      {titulo}
    </h2>
  )
}
