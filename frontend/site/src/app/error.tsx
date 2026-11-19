'use client'

// Se a API falhar ao abrir uma página, mostra-se isto em vez de um erro técnico (SEG26).
export default function Erro({ reset }: { reset: () => void }) {
  return (
    <main className="grid min-h-screen place-items-center px-5">
      <div className="text-center">
        <h1 className="text-3xl font-semibold tracking-tight">Algo correu mal</h1>
        <p className="mt-4 text-stone-600 dark:text-stone-400">O conteúdo não está disponível neste momento.</p>
        <button type="button" onClick={reset} className="mt-8 rounded-full bg-stone-900 px-6 py-3 text-sm font-medium text-white dark:bg-white dark:text-stone-900">
          Tentar de novo
        </button>
      </div>
    </main>
  )
}
