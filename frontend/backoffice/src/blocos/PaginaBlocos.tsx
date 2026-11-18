import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router'
import { type Bloco, api } from '@/api/api'
import { ItemOrdenavel, ListaOrdenavel } from '@/components/Ordenavel'
import { Button } from '@/components/ui/button'
import { Switch } from '@/components/ui/campos'
import { ConfirmarDialogo } from '@/components/ui/dialogo'

// Blocos do site (RF04): arrastar para reordenar, mostrar ou ocultar, editar e remover.
export function PaginaBlocos() {
  const cache = useQueryClient()
  const blocos = useQuery({ queryKey: ['blocos'], queryFn: api.blocos })
  const atualizar = () => cache.invalidateQueries({ queryKey: ['blocos'] })

  const reordenar = useMutation({
    mutationFn: api.reordenarBlocos,
    // Mostra logo a nova ordem; se a API recusar, volta a ler a lista.
    onMutate: (ids) =>
      cache.setQueryData<Bloco[]>(['blocos'], (atuais) => ids.map((id) => atuais!.find((b) => b.id === id)!)),
    onSettled: atualizar,
  })
  const alternar = useMutation({
    mutationFn: (b: Bloco) => api.editarBloco(b.id, { titulo: b.titulo, conteudo: b.conteudo, publico: !b.publico }),
    onSettled: atualizar,
  })
  const remover = useMutation({ mutationFn: api.removerBloco, onSettled: atualizar })
  const erro = reordenar.error ?? alternar.error ?? remover.error ?? blocos.error

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Blocos do site</h1>
        <Button asChild><Link to="/blocos/novo">Novo bloco</Link></Button>
      </div>
      <p className="text-sm text-zinc-600">Arraste para mudar a ordem no site. Os blocos ocultos ficam guardados mas não aparecem.</p>
      {erro && <p role="alert" className="text-sm text-red-600">{erro.message}</p>}
      {blocos.data?.length === 0 && <p className="text-sm text-zinc-500">Ainda não há blocos.</p>}
      {blocos.data && (
        <ListaOrdenavel ids={blocos.data.map((b) => b.id)} onReordenar={(ids) => reordenar.mutate(ids)}>
          <ul className="flex flex-col gap-2">
            {blocos.data.map((bloco) => (
              <ItemOrdenavel key={bloco.id} id={bloco.id} rotulo={bloco.titulo} className="flex items-center gap-3 rounded-lg border border-zinc-200 bg-white p-3">
                <div className="flex-1">
                  <Link to={`/blocos/${bloco.id}`} className="font-medium hover:underline">{bloco.titulo}</Link>
                  <p className="text-xs text-zinc-500">#{bloco.slug}</p>
                </div>
                <label className="flex items-center gap-2 text-sm">
                  <Switch checked={bloco.publico} onCheckedChange={() => alternar.mutate(bloco)} aria-label={`Mostrar ${bloco.titulo} no site`} />
                  {bloco.publico ? 'Visível' : 'Oculto'}
                </label>
                <ConfirmarDialogo titulo={`Remover "${bloco.titulo}"?`} descricao="O bloco sai do site e não pode ser recuperado." onConfirmar={() => remover.mutate(bloco.id)}>
                  <Button variant="ghost" size="sm">Remover</Button>
                </ConfirmarDialogo>
              </ItemOrdenavel>
            ))}
          </ul>
        </ListaOrdenavel>
      )}
    </section>
  )
}
