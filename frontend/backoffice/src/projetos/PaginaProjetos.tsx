import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { api } from '@/api/api'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/cartao'

// Todos os projetos, públicos e não públicos (RF09).
export function PaginaProjetos() {
  const projetos = useQuery({ queryKey: ['projetos'], queryFn: api.projetos })

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Projetos</h1>
        <Button asChild><Link to="/projetos/novo">Novo projeto</Link></Button>
      </div>
      {projetos.error && <p role="alert" className="text-sm text-red-600">{projetos.error.message}</p>}
      {projetos.data?.length === 0 && <p className="text-sm text-zinc-500">Ainda não há projetos.</p>}
      <ul className="grid gap-3 sm:grid-cols-2">
        {projetos.data?.map((p) => (
          <li key={p.id} className="flex gap-3 rounded-lg border border-zinc-200 bg-white p-3">
            {p.imagens[0] ? (
              <img src={p.imagens[0].url} alt="" className="size-16 rounded object-cover" />
            ) : (
              <div className="size-16 rounded bg-zinc-100" />
            )}
            <div className="flex min-w-0 flex-1 flex-col gap-1">
              <Link to={`/projetos/${p.id}`} className="truncate font-medium hover:underline">{p.nome}</Link>
              <span className="truncate text-xs text-zinc-500">/projetos/{p.slug}</span>
              <Badge className={p.publico ? 'w-fit border-emerald-200 bg-emerald-50 text-emerald-700' : 'w-fit border-zinc-200 bg-zinc-50 text-zinc-600'}>
                {p.publico ? 'Público' : 'Não público'}
              </Badge>
            </div>
          </li>
        ))}
      </ul>
    </section>
  )
}
