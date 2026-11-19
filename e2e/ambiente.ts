import path from 'node:path'

// Endereços e credenciais do ambiente de ponta a ponta. Portas pouco usadas (e configuráveis),
// diferentes das de desenvolvimento, para poderem correr ao lado deste e de outros projetos.
export const PORTA_API = process.env.E2E_PORTA_API ?? '57103'
export const PORTA_BACKOFFICE = process.env.E2E_PORTA_BACKOFFICE ?? '55273'
export const PORTA_SITE = process.env.E2E_PORTA_SITE ?? '53100'
export const API = `https://localhost:${PORTA_API}`
export const BACKOFFICE = `http://localhost:${PORTA_BACKOFFICE}`
export const SITE = `http://localhost:${PORTA_SITE}`

export const CONTA = { nome: 'Teste E2E', email: 'e2e@portfolio.test', password: 'password-de-teste-e2e' }

const raiz = path.resolve(import.meta.dirname, '..')
export const caminho = (...partes: string[]) => path.join(raiz, ...partes)

// Porta pouco usada, para não chocar com outros PostgreSQL na mesma máquina.
export const PORTA_BD = process.env.E2E_PORTA_BD ?? '55432'

const ligacao = (utilizador: string, password: string) =>
  `Host=localhost;Port=${PORTA_BD};Database=portfolio;Username=${utilizador};Password=${password}`

// Variáveis de ambiente da API (sobrepõem-se aos user-secrets de desenvolvimento).
export const ambienteApi: Record<string, string> = {
  ASPNETCORE_ENVIRONMENT: 'Development',
  ConnectionStrings__Leitura: ligacao('portfolio_leitura', 'e2e-leitura'),
  ConnectionStrings__Escrita: ligacao('portfolio_escrita', 'e2e-escrita'),
  ConnectionStrings__Migracoes: ligacao('portfolio_migracoes', 'e2e-migracoes'),
  Imagens__Pasta: caminho('e2e', '.armazenamento'),
}

export const compose = `docker compose -p portfolio-e2e -f ${caminho('e2e', 'docker-compose.yml')}`
