import { execSync } from 'node:child_process'
import { rmSync } from 'node:fs'
import { CONTA, PORTA_BD, ambienteApi, caminho, compose } from './ambiente'

const executar = (comando: string, input?: string) =>
  execSync(comando, {
    stdio: input ? ['pipe', 'inherit', 'inherit'] : 'inherit',
    input,
    env: { ...process.env, ...ambienteApi, E2E_PORTA_BD: PORTA_BD },
  })

// Antes dos testes: base de dados nova, migrações e a conta (com o comando criar-admin, como em produção).
export default function preparar() {
  rmSync(ambienteApi.Imagens__Pasta!, { recursive: true, force: true })
  executar(`${compose} up -d --wait`)
  const projeto = caminho('backend', 'src', 'Portfolio.Api')
  executar(`dotnet run --project ${projeto} --no-launch-profile -- migrar`)
  executar(`dotnet run --project ${projeto} --no-launch-profile -- criar-admin`, `${CONTA.nome}\n${CONTA.email}\n${CONTA.password}\n${CONTA.password}\n`)
}
