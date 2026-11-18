import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router'
import { z } from 'zod'
import { api } from '@/api/api'
import { HtmlDaApi } from '@/components/HtmlDaApi'
import { Button } from '@/components/ui/button'
import { ErroCampo, Input, Label, Switch, Textarea } from '@/components/ui/campos'
import { aplicarErrosApi } from '@/lib/formularios'

const esquema = z.object({
  titulo: z.string().trim().min(1, 'Indique o título.').max(150, 'No máximo 150 caracteres.'),
  conteudo: z.string().max(100_000, 'O texto é demasiado comprido.'),
  publico: z.boolean(),
})
type Dados = z.infer<typeof esquema>
const campos = ['titulo', 'conteudo', 'publico'] as const

// Criar (/blocos/novo) ou editar (/blocos/:id) um bloco, com pré-visualização do Markdown.
export function PaginaBloco() {
  const { id } = useParams()
  const novo = id === undefined
  const navegar = useNavigate()
  const cache = useQueryClient()
  const blocos = useQuery({ queryKey: ['blocos'], queryFn: api.blocos, enabled: !novo })
  const bloco = blocos.data?.find((b) => b.id === Number(id))
  const [erro, setErro] = useState<string>()

  const { register, handleSubmit, control, reset, setError, formState } = useForm<Dados>({
    resolver: zodResolver(esquema),
    defaultValues: { titulo: '', conteudo: '', publico: false },
  })
  useEffect(() => {
    if (bloco) reset({ titulo: bloco.titulo, conteudo: bloco.conteudo, publico: bloco.publico })
  }, [bloco, reset])

  const gravar = handleSubmit(async (dados) => {
    setErro(undefined)
    try {
      await (novo ? api.criarBloco(dados) : api.editarBloco(Number(id), dados))
      await cache.invalidateQueries({ queryKey: ['blocos'] })
      navegar('/blocos')
    } catch (e) {
      setErro(aplicarErrosApi(e, campos, setError))
    }
  })

  if (!novo && blocos.isSuccess && !bloco) return <p role="alert">Bloco não encontrado. <Link to="/blocos" className="underline">Voltar</Link></p>

  return (
    <form onSubmit={gravar} className="flex flex-col gap-4" noValidate>
      <h1 className="text-2xl font-semibold">{novo ? 'Novo bloco' : 'Editar bloco'}</h1>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="titulo">Título</Label>
        <Input id="titulo" aria-invalid={!!formState.errors.titulo} aria-describedby="titulo-erro" {...register('titulo')} />
        <ErroCampo id="titulo-erro" mensagem={formState.errors.titulo?.message} />
      </div>
      <Controller
        name="publico"
        control={control}
        render={({ field }) => (
          <label className="flex items-center gap-2 text-sm">
            <Switch checked={field.value} onCheckedChange={field.onChange} aria-label="Visível no site" />
            Visível no site
          </label>
        )}
      />
      <div className="grid gap-4 md:grid-cols-2">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="conteudo">Texto (Markdown)</Label>
          <Textarea id="conteudo" rows={16} aria-describedby="conteudo-erro" {...register('conteudo')} />
          <ErroCampo id="conteudo-erro" mensagem={formState.errors.conteudo?.message} />
        </div>
        <PreVisualizacao control={control} />
      </div>
      {erro && <p role="alert" className="text-sm text-red-600">{erro}</p>}
      <div className="flex gap-2">
        <Button type="submit" disabled={formState.isSubmitting}>Gravar</Button>
        <Button variant="outline" asChild><Link to="/blocos">Cancelar</Link></Button>
      </div>
    </form>
  )
}

// O HTML vem da API, já sanitizado: é exatamente o que o site vai mostrar (SEG21).
function PreVisualizacao({ control }: { control: ReturnType<typeof useForm<Dados>>['control'] }) {
  const texto = useWatch({ control, name: 'conteudo' })
  const [atrasado, setAtrasado] = useState(texto)
  useEffect(() => {
    const espera = setTimeout(() => setAtrasado(texto), 400)
    return () => clearTimeout(espera)
  }, [texto])
  const html = useQuery({ queryKey: ['pre-visualizar', atrasado], queryFn: () => api.preVisualizar(atrasado), enabled: atrasado.length > 0 })

  return (
    <div className="flex flex-col gap-1.5">
      <span className="text-sm font-medium">Pré-visualização</span>
      <div className="min-h-32 rounded-md border border-zinc-200 bg-white p-3" aria-label="Pré-visualização">
        {html.data && <HtmlDaApi html={html.data.html} className="markdown" />}
      </div>
    </div>
  )
}
