import path from 'node:path'

// Endereços e credenciais do ambiente de ponta a ponta. Portas diferentes das de
// desenvolvimento (7003, 5173, 3000, 5432), para os dois poderem correr ao mesmo tempo.
export const API = 'https://localhost:7103'
export const BACKOFFICE = 'http://localhost:5273'
export const SITE = 'http://localhost:3100'

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
