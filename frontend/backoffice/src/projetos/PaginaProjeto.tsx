import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router'
import { z } from 'zod'
import { type Projeto, api } from '@/api/api'
import { Button } from '@/components/ui/button'
import { ErroCampo, Input, Label, Switch, Textarea } from '@/components/ui/campos'
import { ConfirmarDialogo } from '@/components/ui/dialogo'
import { aplicarErrosApi, formatoGithub } from '@/lib/formularios'
import { ImagensProjeto } from './ImagensProjeto'
import { TimelineProjeto } from './TimelineProjeto'

const esquema = z.object({
  nome: z.string().trim().min(1, 'Indique o nome.').max(150, 'No máximo 150 caracteres.'),
  descricao: z.string().max(10_000, 'A descrição é demasiado comprida.'),
  repositorioGithub: z
    .string()
    .trim()
    .refine((v) => v === '' || formatoGithub.test(v), 'Tem de ser o link de um repositório do GitHub, por exemplo https://github.com/dono/repositorio.'),
})
type Dados = z.infer<typeof esquema>
const campos = ['nome', 'descricao', 'repositorioGithub'] as const

// Criar (/projetos/novo) ou editar (/projetos/:id) um projeto (RF06–RF08, RF10, RF13).
export function PaginaProjeto() {
  const { id } = useParams()
  const projetoId = id === undefined ? undefined : Number(id)
  const projeto = useQuery({ queryKey: ['projeto', projetoId], queryFn: () => api.projeto(projetoId!), enabled: projetoId !== undefined, retry: false })

  if (projeto.error) return <p role="alert">{projeto.error.message} <Link to="/projetos" className="underline">Voltar</Link></p>
  if (projetoId !== undefined && !projeto.data) return <p className="text-sm text-zinc-500">A carregar…</p>

  return (
    <div className="flex flex-col gap-8">
      <FormularioProjeto projeto={projeto.data} />
      {projeto.data && (
        <>
          <ImagensProjeto projeto={projeto.data} />
          <TimelineProjeto projetoId={projeto.data.id} />
        </>
      )}
    </div>
  )
}

function FormularioProjeto({ projeto }: { projeto?: Projeto }) {
  const navegar = useNavigate()
  const cache = useQueryClient()
  const [erro, setErro] = useState<string>()
  const { register, handleSubmit, reset, setError, formState } = useForm<Dados>({
    resolver: zodResolver(esquema),
    defaultValues: { nome: '', descricao: '', repositorioGithub: '' },
  })
  useEffect(() => {
    if (projeto) reset({ nome: projeto.nome, descricao: projeto.descricao ?? '', repositorioGithub: projeto.repositorioGithub ?? '' })
  }, [projeto, reset])

  const atualizar = (p: Projeto) => {
    cache.setQueryData(['projeto', p.id], p)
    return cache.invalidateQueries({ queryKey: ['projetos'] })
  }
  const publicar = useMutation({ mutationFn: (publico: boolean) => api.publicarProjeto(projeto!.id, publico), onSuccess: atualizar })
  const remover = useMutation({
    mutationFn: () => api.removerProjeto(projeto!.id),
    onSuccess: async () => {
      await cache.invalidateQueries({ queryKey: ['projetos'] })
      navegar('/projetos')
    },
  })

  const gravar = handleSubmit(async ({ nome, descricao, repositorioGithub }) => {
    setErro(undefined)
    const dados = { nome, descricao: descricao.trim() || null, repositorioGithub: repositorioGithub || null }
    try {
      const gravado = await (projeto ? api.editarProjeto(projeto.id, dados) : api.criarProjeto(dados))
      await atualizar(gravado)
      if (!projeto) navegar(`/projetos/${gravado.id}`)
    } catch (e) {
      setErro(aplicarErrosApi(e, campos, setError))
    }
  })

  const mensagem = erro ?? publicar.error?.message ?? remover.error?.message

  return (
    <form onSubmit={gravar} className="flex flex-col gap-4" noValidate>
      <Cabecalho projeto={projeto} onPublicar={(v) => publicar.mutate(v)} />
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="nome">Nome</Label>
        <Input id="nome" aria-invalid={!!formState.errors.nome} aria-describedby="nome-erro" {...register('nome')} />
        <ErroCampo id="nome-erro" mensagem={formState.errors.nome?.message} />
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="descricao">Descrição</Label>
        <Textarea id="descricao" rows={4} className="font-sans" aria-describedby="descricao-erro" {...register('descricao')} />
        <ErroCampo id="descricao-erro" mensagem={formState.errors.descricao?.message} />
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="repositorioGithub">Repositório no GitHub (opcional)</Label>
        <Input id="repositorioGithub" placeholder="https://github.com/dono/repositorio" aria-invalid={!!formState.errors.repositorioGithub} aria-describedby="repositorio-erro" {...register('repositorioGithub')} />
        <ErroCampo id="repositorio-erro" mensagem={formState.errors.repositorioGithub?.message} />
        <p className="text-xs text-zinc-500">A timeline do projeto são os commits deste repositório. Num projeto público, as mensagens dos commits ficam visíveis no site.</p>
      </div>
      {mensagem && <p role="alert" className="text-sm text-red-600">{mensagem}</p>}
      <Acoes projeto={projeto} aGravar={formState.isSubmitting} onRemover={() => remover.mutate()} />
    </form>
  )
}

function Cabecalho({ projeto, onPublicar }: { projeto?: Projeto; onPublicar: (publico: boolean) => void }) {
  if (!projeto) return <h1 className="text-2xl font-semibold">Novo projeto</h1>
  return (
    <>
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="text-2xl font-semibold">{projeto.nome}</h1>
        <label className="ml-auto flex items-center gap-2 text-sm">
          <Switch checked={projeto.publico} onCheckedChange={onPublicar} aria-label="Público no site" />
          {projeto.publico ? 'Público no site' : 'Não público'}
        </label>
      </div>
      <p className="text-sm text-zinc-500">Endereço no site: /projetos/{projeto.slug}</p>
    </>
  )
}

function Acoes({ projeto, aGravar, onRemover }: { projeto?: Projeto; aGravar: boolean; onRemover: () => void }) {
  return (
    <div className="flex gap-2">
      <Button type="submit" disabled={aGravar}>{projeto ? 'Gravar' : 'Criar projeto'}</Button>
      <Button variant="outline" asChild><Link to="/projetos">Voltar</Link></Button>
      {projeto && (
        <ConfirmarDialogo titulo={`Remover "${projeto.nome}"?`} descricao="O projeto e as suas imagens são apagados e não podem ser recuperados." onConfirmar={onRemover}>
          <Button type="button" variant="destructive" className="ml-auto">Remover projeto</Button>
        </ConfirmarDialogo>
      )}
    </div>
  )
}
