import { rmSync } from 'node:fs'
import { homedir } from 'node:os'
import { defineConfig, devices } from '@playwright/test'
import { API, BACKOFFICE, SITE, ambienteApi, caminho } from './ambiente'

// O site guarda em disco a cache dos dados da API (ISR) entre execuções. Cada execução cria uma base
// de dados nova, por isso a cache da anterior mostraria conteúdo que já não existe (por exemplo uma
// imagem com outro nome). Apaga-se aqui, antes de os servidores arrancarem.
rmSync(caminho('frontend', 'site', '.next-e2e'), { recursive: true, force: true })

// O fluxo principal com tudo a correr de verdade: PostgreSQL, API, backoffice e site.
export default defineConfig({
  testDir: 'testes',
  timeout: 180_000,
  expect: { timeout: 20_000 },
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  globalSetup: './preparar.ts',
  globalTeardown: './terminar.ts',
  use: {
    ...devices['Desktop Chrome'],
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  webServer: [
    {
      // A API corre em HTTPS, como em desenvolvimento (o token CSRF só circula por HTTPS).
      command: `dotnet run --project ${caminho('backend', 'src', 'Portfolio.Api')} --no-launch-profile --urls ${API}`,
      url: `${API}/api/backoffice/auth/csrf`,
      ignoreHTTPSErrors: true,
      env: ambienteApi,
      // O registo da API aparece na saída, para se perceber qualquer erro 500.
      stdout: 'pipe',
      timeout: 180_000,
      reuseExistingServer: false,
    },
    {
      command: 'npm run dev -- --port 5273',
      cwd: caminho('frontend', 'backoffice'),
      url: BACKOFFICE,
      env: { API_URL: API },
      timeout: 120_000,
      reuseExistingServer: false,
    },
    {
      command: 'npx next dev --port 3100',
      cwd: caminho('frontend', 'site'),
      url: SITE,
      env: {
        API_URL: API,
        SITE_URL: SITE,
        NEXT_DIST_DIR: '.next-e2e',
        // O Node confia no certificado de desenvolvimento do .NET (ver README).
        NODE_EXTRA_CA_CERTS: process.env.NODE_EXTRA_CA_CERTS ?? `${homedir()}/.aspnet/https/portfolio-dev.pem`,
      },
      timeout: 180_000,
      reuseExistingServer: false,
    },
  ],
})
