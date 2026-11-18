import { useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { useNavigate } from 'react-router'
import { api } from '@/api/api'
import { ErroApi } from '@/api/cliente'
import { Button } from '@/components/ui/button'
import { Input, Label } from '@/components/ui/campos'
import { PaginaCentrada } from './PaginaLogin'

// Segundo passo do login: o código da app autenticadora ou um código de recuperação (SEG09).
export function PaginaCodigo() {
  const navegar = useNavigate()
  const cache = useQueryClient()
  const [codigo, setCodigo] = useState('')
  const [erro, setErro] = useState<string>()
  const [aEnviar, setAEnviar] = useState(false)

  async function confirmar(evento: FormEvent) {
    evento.preventDefault()
    setErro(undefined)
    setAEnviar(true)
    try {
      await api.codigo(codigo.trim())
      await cache.invalidateQueries({ queryKey: ['eu'] })
      navegar('/')
    } catch (e) {
      // Ao fim de 5 códigos errados, ou se a sessão parcial expirou, recomeça-se pela password.
      if (e instanceof ErroApi && e.estado === 401 && e.message !== 'Código inválido') {
        navegar('/login', { state: { aviso: e.message.startsWith('Demasiadas') ? e.message : 'O tempo para introduzir o código terminou. Entre de novo.' } })
        return
      }
      setErro(e instanceof ErroApi ? e.message : 'Ocorreu um erro. Tente de novo.')
    } finally {
      setAEnviar(false)
    }
  }

  return (
    <PaginaCentrada titulo="Código de verificação">
      <form onSubmit={confirmar} className="flex flex-col gap-4">
        <p className="text-sm text-zinc-600">Introduza o código de 6 dígitos da app autenticadora, ou um código de recuperação.</p>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="codigo">Código</Label>
          <Input id="codigo" autoComplete="one-time-code" inputMode="text" autoFocus maxLength={32} value={codigo} onChange={(e) => setCodigo(e.target.value)} />
        </div>
        {erro && <p role="alert" className="text-sm text-red-600">{erro}</p>}
        <Button type="submit" disabled={aEnviar || codigo.trim().length === 0}>Confirmar</Button>
      </form>
    </PaginaCentrada>
  )
}
