import { execSync } from 'node:child_process'
import { rmSync } from 'node:fs'
import { PORTA_BD, ambienteApi, compose } from './ambiente'

// Depois dos testes: apaga a base de dados e as imagens.
// Com E2E_MANTER=1 ficam, para se poder investigar uma falha.
export default function terminar() {
  if (process.env.E2E_MANTER) return
  execSync(`${compose} down -v`, { stdio: 'inherit', env: { ...process.env, E2E_PORTA_BD: PORTA_BD } })
  rmSync(ambienteApi.Imagens__Pasta!, { recursive: true, force: true })
}
