import type { Metadata } from 'next'
import { Aviso, Cabecalho, Rodape, TituloSecao, ligacoesDoMenu } from '@/componentes/Estrutura'
import { CartaoProjeto, Heroi, SecaoBloco } from '@/componentes/Inicio'
import { REVALIDAR_SEGUNDOS, api, nomeSite, ouIndisponivel } from '@/lib/api'
import { textoSimples } from '@/lib/texto'

// ISR: no máximo a cada 60 segundos o servidor volta a pedir os dados à API.
export const revalidate = REVALIDAR_SEGUNDOS

export async function generateMetadata(): Promise<Metadata> {
  const blocos = await ouIndisponivel(api.blocos())
  const primeiro = blocos === 'indisponivel' ? undefined : blocos[0]
  const descricao = primeiro ? textoSimples(primeiro.html) : undefined
  return {
    title: { absolute: primeiro ? `${nomeSite()} — ${primeiro.titulo}` : nomeSite() },
    description: descricao,
    openGraph: { title: primeiro?.titulo ?? nomeSite(), description: descricao, url: '/' },
  }
}

// Página inicial (RF05, RF09): os blocos pela ordem do backoffice e os projetos públicos.
export default async function Inicio() {
  const [blocos, projetos] = await Promise.all([ouIndisponivel(api.blocos()), ouIndisponivel(api.projetos())])
  const listaBlocos = blocos === 'indisponivel' ? [] : blocos
  const [primeiro, ...outros] = listaBlocos

  return (
    <>
      <Cabecalho nome={nomeSite()} ligacoes={ligacoesDoMenu(listaBlocos)} />
      <main>
        <Heroi bloco={primeiro} />
        {blocos === 'indisponivel' && (
          <div className="mx-auto max-w-6xl px-5 sm:px-8"><Aviso>O conteúdo não está disponível neste momento. Volte daqui a pouco.</Aviso></div>
        )}
        {outros.map((bloco, i) => <SecaoBloco key={bloco.slug} bloco={bloco} numero={i + 1} />)}
        <section aria-labelledby="titulo-projetos" id="projetos" className="scroll-mt-20 border-t border-stone-200 dark:border-stone-800">
          <div className="mx-auto max-w-6xl px-5 py-20 sm:px-8">
            <TituloSecao id="titulo-projetos" numero={String(outros.length + 1).padStart(2, '0')} titulo="Projetos" />
            <p className="mt-4 max-w-2xl text-3xl font-semibold tracking-tight text-balance text-stone-900 sm:text-4xl dark:text-white">
              Coisas que construí, com o histórico de cada uma.
            </p>
            <div className="mt-12">
              {projetos === 'indisponivel' ? (
                <Aviso>Os projetos não estão disponíveis neste momento. Volte daqui a pouco.</Aviso>
              ) : projetos.length === 0 ? (
                <p className="text-stone-500">Em breve.</p>
              ) : (
                <ul className="grid gap-6 md:grid-cols-2">
                  {projetos.map((p) => <CartaoProjeto key={p.slug} projeto={p} />)}
                </ul>
              )}
            </div>
          </div>
        </section>
      </main>
      <Rodape nome={nomeSite()} />
    </>
  )
}
