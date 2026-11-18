import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useLocation, useNavigate } from 'react-router'
import { z } from 'zod'
import { api } from '@/api/api'
import { ErroApi } from '@/api/cliente'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/cartao'
import { ErroCampo, Input, Label } from '@/components/ui/campos'

const esquema = z.object({
  email: z.string().trim().min(1, 'Indique o email.').max(254),
  password: z.string().min(1, 'Indique a password.').max(256),
})
type Dados = z.infer<typeof esquema>

export function PaginaLogin() {
  const navegar = useNavigate()
  const aviso = (useLocation().state as { aviso?: string } | null)?.aviso
  const [erro, setErro] = useState<string>()
  const { register, handleSubmit, formState } = useForm<Dados>({ resolver: zodResolver(esquema) })

  const entrar = handleSubmit(async ({ email, password }) => {
    setErro(undefined)
    try {
      const { proximoPasso } = await api.login(email, password)
      navegar(proximoPasso === 'codigo' ? '/login/2fa' : '/configurar-2fa')
    } catch (e) {
      // A API dá sempre a mesma mensagem, sem dizer se foi o email ou a password (SEG04).
      setErro(e instanceof ErroApi ? e.message : 'Ocorreu um erro. Tente de novo.')
    }
  })

  return (
    <PaginaCentrada titulo="Entrar no backoffice">
      <form onSubmit={entrar} className="flex flex-col gap-4" noValidate>
        {aviso && <p className="rounded-md bg-amber-50 p-3 text-sm text-amber-800">{aviso}</p>}
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="email">Email</Label>
          <Input id="email" type="email" autoComplete="username" aria-invalid={!!formState.errors.email} aria-describedby="email-erro" {...register('email')} />
          <ErroCampo id="email-erro" mensagem={formState.errors.email?.message} />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="password">Password</Label>
          <Input id="password" type="password" autoComplete="current-password" aria-invalid={!!formState.errors.password} aria-describedby="password-erro" {...register('password')} />
          <ErroCampo id="password-erro" mensagem={formState.errors.password?.message} />
        </div>
        {erro && <p role="alert" className="text-sm text-red-600">{erro}</p>}
        <Button type="submit" disabled={formState.isSubmitting}>Entrar</Button>
      </form>
    </PaginaCentrada>
  )
}

export function PaginaCentrada({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <main className="flex min-h-screen items-center justify-center p-4">
      <Card className="w-full max-w-sm">
        <CardHeader><CardTitle>{titulo}</CardTitle></CardHeader>
        <CardContent>{children}</CardContent>
      </Card>
    </main>
  )
}
