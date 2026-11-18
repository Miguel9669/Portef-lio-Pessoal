import { rectSortingStrategy } from '@dnd-kit/sortable'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { type ChangeEvent, useState } from 'react'
import { type Projeto, api } from '@/api/api'
import { ItemOrdenavel, ListaOrdenavel } from '@/components/Ordenavel'
import { Button } from '@/components/ui/button'
import { ConfirmarDialogo } from '@/components/ui/dialogo'

// Imagens do projeto (RF13). A validação a sério é na API (SEG17–SEG19); aqui só se filtra a escolha.
export function ImagensProjeto({ projeto }: { projeto: Projeto }) {
  const cache = useQueryClient()
  const [erro, setErro] = useState<string>()
  const atualizar = () => cache.invalidateQueries({ queryKey: ['projeto', projeto.id] })

  const enviar = useMutation({
    mutationFn: async (ficheiros: File[]) => {
      for (const ficheiro of ficheiros) await api.enviarImagem(projeto.id, ficheiro)
    },
    onError: (e) => setErro(e.message),
    onSettled: atualizar,
  })
  const reordenar = useMutation({ mutationFn: (ids: number[]) => api.reordenarImagens(projeto.id, ids), onError: (e) => setErro(e.message), onSettled: atualizar })
  const remover = useMutation({ mutationFn: api.removerImagem, onError: (e) => setErro(e.message), onSettled: atualizar })

  function escolher(evento: ChangeEvent<HTMLInputElement>) {
    setErro(undefined)
    const ficheiros = Array.from(evento.target.files ?? [])
    evento.target.value = ''
    if (ficheiros.length > 0) enviar.mutate(ficheiros)
  }

  return (
    <section className="flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Imagens</h2>
        <Button asChild variant="outline" size="sm">
          <label className="cursor-pointer">
            {enviar.isPending ? 'A enviar…' : 'Adicionar imagens'}
            <input type="file" accept="image/jpeg,image/png,image/webp" multiple className="sr-only" onChange={escolher} disabled={enviar.isPending} aria-label="Adicionar imagens" />
          </label>
        </Button>
      </div>
      <p className="text-xs text-zinc-500">JPEG, PNG ou WebP, até 5 MB. A localização GPS e os outros metadados são removidos. Arraste para mudar a ordem.</p>
      {erro && <p role="alert" className="text-sm text-red-600">{erro}</p>}
      {projeto.imagens.length > 0 && (
        <ListaOrdenavel ids={projeto.imagens.map((i) => i.id)} onReordenar={(ids) => reordenar.mutate(ids)} estrategia={rectSortingStrategy}>
          <ul className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            {projeto.imagens.map((imagem) => (
              <ItemOrdenavel key={imagem.id} id={imagem.id} rotulo={imagem.nomeOriginal} className="flex flex-col gap-2 rounded-lg border border-zinc-200 bg-white p-2">
                <img src={imagem.url} alt={imagem.nomeOriginal} className="aspect-square w-full rounded object-cover" />
                <div className="flex items-center gap-1">
                  <span className="flex-1 truncate text-xs text-zinc-600">{imagem.nomeOriginal}</span>
                  <ConfirmarDialogo titulo="Remover a imagem?" descricao={imagem.nomeOriginal} onConfirmar={() => remover.mutate(imagem.id)}>
                    <Button variant="ghost" size="sm">Remover</Button>
                  </ConfirmarDialogo>
                </div>
              </ItemOrdenavel>
            ))}
          </ul>
        </ListaOrdenavel>
      )}
    </section>
  )
}
