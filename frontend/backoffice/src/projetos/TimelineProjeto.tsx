import { useQuery } from '@tanstack/react-query'
import { type EstadoTimeline, api } from '@/api/api'

const avisos: Record<EstadoTimeline, string | undefined> = {
  atual: undefined,
  desatualizada: 'O GitHub não respondeu: estes são os commits da última vez que respondeu.',
  indisponivel: 'O GitHub não está a responder. Tente daqui a pouco.',
  repositorioNaoEncontrado: 'O repositório não foi encontrado no GitHub (não existe ou é privado).',
  semRepositorio: 'Indique o repositório do GitHub para ver a timeline.',
}

const formatoData = new Intl.DateTimeFormat('pt-PT', { dateStyle: 'medium', timeStyle: 'short' })

// Commits do repositório do projeto (RF11, RF12). As mensagens vêm do GitHub e são
// mostradas como texto, nunca como HTML (SEG29). Os links são sempre do github.com.
export function TimelineProjeto({ projetoId }: { projetoId: number }) {
  const timeline = useQuery({ queryKey: ['timeline', projetoId], queryFn: () => api.timeline(projetoId) })
  const aviso = timeline.data && avisos[timeline.data.estado]

  return (
    <section className="flex flex-col gap-3">
      <h2 className="text-lg font-semibold">Timeline (commits do GitHub)</h2>
      {timeline.error && <p role="alert" className="text-sm text-red-600">{timeline.error.message}</p>}
      {aviso && <p className="text-sm text-zinc-600">{aviso}</p>}
      <ol className="flex flex-col border-l border-zinc-200">
        {timeline.data?.commits.map((commit) => (
          <li key={commit.sha} className="relative ml-4 py-2">
            <span className="absolute top-3.5 -left-[1.3rem] size-2 rounded-full bg-zinc-400" />
            {commit.url.startsWith('https://github.com/') ? (
              <a href={commit.url} target="_blank" rel="noopener noreferrer" className="font-medium hover:underline">{commit.mensagem}</a>
            ) : (
              <span className="font-medium">{commit.mensagem}</span>
            )}
            <p className="text-xs text-zinc-500">
              {commit.autor} · {formatoData.format(new Date(commit.data))} · <code>{commit.sha.slice(0, 7)}</code>
            </p>
          </li>
        ))}
      </ol>
    </section>
  )
}
