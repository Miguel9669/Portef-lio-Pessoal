import { Navigate, Route, Routes } from 'react-router'
import { PaginaPainel } from './PaginaPainel'
import { Layout } from './autenticacao/Layout'
import { PaginaCodigo } from './autenticacao/PaginaCodigo'
import { PaginaConfigurar2fa } from './autenticacao/PaginaConfigurar2fa'
import { PaginaLogin } from './autenticacao/PaginaLogin'
import { PaginaBloco } from './blocos/PaginaBloco'
import { PaginaBlocos } from './blocos/PaginaBlocos'
import { PaginaProjeto } from './projetos/PaginaProjeto'
import { PaginaProjetos } from './projetos/PaginaProjetos'

// Rotas de docs/04-componentes.md. Tudo dentro do Layout exige sessão completa.
export function App() {
  return (
    <Routes>
      <Route path="/login" element={<PaginaLogin />} />
      <Route path="/login/2fa" element={<PaginaCodigo />} />
      <Route path="/configurar-2fa" element={<PaginaConfigurar2fa />} />
      <Route element={<Layout />}>
        <Route index element={<PaginaPainel />} />
        <Route path="blocos" element={<PaginaBlocos />} />
        <Route path="blocos/novo" element={<PaginaBloco />} />
        <Route path="blocos/:id" element={<PaginaBloco />} />
        <Route path="projetos" element={<PaginaProjetos />} />
        <Route path="projetos/novo" element={<PaginaProjeto />} />
        <Route path="projetos/:id" element={<PaginaProjeto />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
