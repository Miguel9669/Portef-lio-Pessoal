import Link from 'next/link'
import { SetaEsquerda } from '@/componentes/Icones'

export default function NaoEncontrado() {
  return (
    <main className="grid min-h-screen place-items-center px-5">
      <div className="text-center">
        <p className="font-mono text-sm text-violet-600 dark:text-violet-400">404</p>
        <h1 className="mt-3 text-4xl font-semibold tracking-tight sm:text-5xl">Página não encontrada</h1>
        <p className="mt-4 text-stone-600 dark:text-stone-400">O endereço pode estar errado, ou o projeto já não estar publicado.</p>
        <Link href="/" className="group mt-8 inline-flex items-center gap-2 rounded-full bg-stone-900 px-6 py-3 text-sm font-medium text-white dark:bg-white dark:text-stone-900">
          <SetaEsquerda className="size-4 transition group-hover:-translate-x-0.5" />
          Voltar ao início
        </Link>
      </div>
    </main>
  )
}
