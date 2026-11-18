import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { api } from '@/api/api'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/cartao'

export function PaginaPainel() {
  const blocos = useQuery({ queryKey: ['blocos'], queryFn: api.blocos })
  const projetos = useQuery({ queryKey: ['projetos'], queryFn: api.projetos })
  const publicos = projetos.data?.filter((p) => p.publico).length

  return (
    <section className="grid gap-4 sm:grid-cols-2">
      <Card>
        <CardHeader><CardTitle><Link to="/blocos" className="hover:underline">Blocos do site</Link></CardTitle></CardHeader>
        <CardContent className="text-sm text-zinc-600">{blocos.data ? `${blocos.data.length} blocos, ${blocos.data.filter((b) => b.publico).length} visíveis` : '…'}</CardContent>
      </Card>
      <Card>
        <CardHeader><CardTitle><Link to="/projetos" className="hover:underline">Projetos</Link></CardTitle></CardHeader>
        <CardContent className="text-sm text-zinc-600">{projetos.data ? `${projetos.data.length} projetos, ${publicos} públicos` : '…'}</CardContent>
      </Card>
    </section>
  )
}
