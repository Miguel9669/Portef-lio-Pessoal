import type { Metadata } from 'next'
import Link from 'next/link'
import { notFound } from 'next/navigation'
import { Cabecalho, Rodape, TituloSecao, ligacoesDoMenu } from '@/componentes/Estrutura'
import { SetaEsquerda } from '@/componentes/Icones'
import { BotaoGithub, Galeria, TimelineCommits } from '@/componentes/Projeto'
import { api, nomeSite, ouIndisponivel } from '@/lib/api'
import { textoSimples } from '@/lib/texto'

// Tem de ser um número escrito aqui (o Next.js lê-o sem executar o código); o teste garante
// que é igual a REVALIDAR_SEGUNDOS.
export const revalidate = 60

type Props = { params: Promise<{ slug: string }> }

// Pré-visualização do link (LinkedIn, WhatsApp): título, descrição e a primeira imagem do projeto.
export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params
  const projeto = await ouIndisponivel(api.projeto(slug))
  if (!projeto || projeto === 'indisponivel') return { title: 'Projeto' }

  const descricao = projeto.descricao ? textoSimples(projeto.descricao) : undefined
  const imagem = projeto.imagens[0]
  return {
    title: projeto.nome,
    description: descricao,
    alternates: { canonical: `/projetos/${projeto.slug}` },
    openGraph: {
      type: 'article',
      title: projeto.nome,
      description: descricao,
      url: `/projetos/${projeto.slug}`,
      images: imagem ? [{ url: imagem.url, alt: projeto.nome }] : undefined,
    },
    twitter: { card: imagem ? 'summary_large_image' : 'summary', title: projeto.nome, description: descricao },
  }
}

// Página de um projeto público (RF12): descrição, imagens e timeline de commits.
// Um projeto não público dá 404, como um que não existe (SEG16).
export default async function PaginaProjeto({ params }: Props) {
  const { slug } = await params
  const [projeto, blocos, timeline] = await Promise.all([
    api.projeto(slug),
    ouIndisponivel(api.blocos()),
    ouIndisponivel(api.timeline(slug)),
  ])
  if (!projeto) notFound()

  return (
    <>
      <Cabecalho nome={nomeSite()} ligacoes={ligacoesDoMenu(blocos === 'indisponivel' ? [] : blocos)} />
      <main>
        <section className="mx-auto max-w-6xl px-5 pt-16 pb-12 sm:px-8 sm:pt-20">
          <Link href="/#projetos" className="group inline-flex items-center gap-2 text-sm text-stone-500 transition hover:text-stone-900 dark:hover:text-white">
            <SetaEsquerda className="size-4 transition group-hover:-translate-x-0.5" />
            Todos os projetos
          </Link>
          <div className="mt-8">
            <TituloSecao numero="01" titulo="Projeto" />
          </div>
          <h1 className="mt-4 max-w-4xl text-4xl font-semibold tracking-tight text-balance sm:text-6xl">{projeto.nome}</h1>
          {projeto.descricao && (
            <p className="mt-6 max-w-2xl text-xl leading-relaxed whitespace-pre-line text-stone-600 dark:text-stone-300">{projeto.descricao}</p>
          )}
          {projeto.repositorioGithub && (
            <div className="mt-8">
              <BotaoGithub url={projeto.repositorioGithub} />
            </div>
          )}
        </section>
        <Galeria imagens={projeto.imagens} nome={projeto.nome} />
        <TimelineCommits timeline={timeline ?? 'indisponivel'} />
      </main>
      <Rodape nome={nomeSite()} />
    </>
  )
}
