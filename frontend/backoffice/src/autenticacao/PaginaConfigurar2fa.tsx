import { useQuery, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { Navigate, useNavigate } from 'react-router'
import { api } from '@/api/api'
import { ErroApi } from '@/api/cliente'
import { Button } from '@/components/ui/button'
import { Input, Label } from '@/components/ui/campos'
import { PaginaCentrada } from './PaginaLogin'

// Primeiro login: QR code, confirmação do primeiro código e códigos de recuperação (SEG09).
export function PaginaConfigurar2fa() {
  const navegar = useNavigate()
  const cache = useQueryClient()
  // Pedido uma só vez: cada pedido gera um segredo novo e invalida o anterior.
  const configuracao = useQuery({ queryKey: ['configurar-2fa'], queryFn: api.configurar2fa, staleTime: Infinity, gcTime: 0, retry: false })
  const [codigo, setCodigo] = useState('')
  const [erro, setErro] = useState<string>()
  const [codigos, setCodigos] = useState<string[]>()

  async function ativar(evento: FormEvent) {
    evento.preventDefault()
    setErro(undefined)
    try {
      setCodigos((await api.ativar2fa(codigo.trim())).codigosRecuperacao)
    } catch (e) {
      setErro(e instanceof ErroApi ? e.message : 'Ocorreu um erro. Tente de novo.')
    }
  }

  if (configuracao.isError) return <Navigate to="/login" replace state={{ aviso: 'Entre de novo para configurar o 2FA.' }} />

  if (codigos)
    return (
      <PaginaCentrada titulo="Códigos de recuperação">
        <p className="text-sm text-zinc-600">Guarde estes códigos num sítio seguro. Cada um serve uma vez, se perder o telemóvel. Não voltam a ser mostrados.</p>
        <ul className="my-4 grid grid-cols-2 gap-2 font-mono text-sm" aria-label="Códigos de recuperação">
          {codigos.map((c) => <li key={c}>{c}</li>)}
        </ul>
        <Button
          className="w-full"
          onClick={async () => {
            await cache.invalidateQueries({ queryKey: ['eu'] })
            navegar('/')
          }}
        >
          Já guardei os códigos
        </Button>
      </PaginaCentrada>
    )

  return (
    <PaginaCentrada titulo="Configurar a verificação em dois passos">
      {configuracao.data ? (
        <form onSubmit={ativar} className="flex flex-col gap-4">
          <p className="text-sm text-zinc-600">Leia o QR code com o Google Authenticator, Authy ou Microsoft Authenticator.</p>
          <img src={configuracao.data.qrCode} alt="QR code para a app autenticadora" className="mx-auto size-48" />
          <p className="text-xs text-zinc-500">
            Ou escreva o segredo: <code className="break-all">{configuracao.data.segredo}</code>
          </p>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="codigo">Código da app</Label>
            <Input id="codigo" autoComplete="one-time-code" inputMode="numeric" maxLength={6} value={codigo} onChange={(e) => setCodigo(e.target.value)} />
          </div>
          {erro && <p role="alert" className="text-sm text-red-600">{erro}</p>}
          <Button type="submit" disabled={codigo.trim().length !== 6}>Ativar</Button>
        </form>
      ) : (
        <p className="text-sm text-zinc-500">A preparar…</p>
      )}
    </PaginaCentrada>
  )
}
