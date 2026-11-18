import { useQuery, useQueryClient } from '@tanstack/react-query'
import { NavLink, Navigate, Outlet, useNavigate } from 'react-router'
import { api } from '@/api/api'
import { ErroApi } from '@/api/cliente'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

// Rotas protegidas: sem sessão completa (password e 2FA), volta ao login.
export function Layout() {
  const navegar = useNavigate()
  const cache = useQueryClient()
  const eu = useQuery({ queryKey: ['eu'], queryFn: api.eu, retry: false })

  if (eu.isPending) return <p className="p-8 text-sm text-zinc-500">A carregar…</p>
  if (eu.error) {
    const semSessao = eu.error instanceof ErroApi && (eu.error.estado === 401 || eu.error.estado === 403)
    return semSessao ? <Navigate to="/login" replace /> : <p role="alert" className="p-8 text-red-600">{eu.error.message}</p>
  }

  async function sair() {
    await api.logout()
    cache.clear()
    navegar('/login')
  }

  const ligacao = ({ isActive }: { isActive: boolean }) => cn('rounded-md px-3 py-1.5 text-sm', isActive ? 'bg-zinc-900 text-white' : 'hover:bg-zinc-100')

  return (
    <div className="min-h-screen">
      <header className="border-b border-zinc-200 bg-white">
        <nav className="mx-auto flex max-w-5xl items-center gap-2 p-3">
          <span className="mr-4 font-semibold">Backoffice</span>
          <NavLink to="/" end className={ligacao}>Painel</NavLink>
          <NavLink to="/blocos" className={ligacao}>Blocos</NavLink>
          <NavLink to="/projetos" className={ligacao}>Projetos</NavLink>
          <span className="ml-auto text-sm text-zinc-600">{eu.data.nome}</span>
          <Button variant="outline" size="sm" onClick={sair}>Sair</Button>
        </nav>
      </header>
      <main className="mx-auto max-w-5xl p-4 sm:p-6">
        <Outlet />
      </main>
    </div>
  )
}
