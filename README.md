# Portfólio Pessoal

Portfólio pessoal com um site público e um backoffice para gerir o conteúdo: blocos de texto do site, projetos e, em cada projeto, uma timeline com os commits do seu repositório no GitHub.

| Componente | Tecnologia | Pasta |
| --- | --- | --- |
| API | .NET 10, ASP.NET Core, Entity Framework Core | `backend/` |
| Base de dados | PostgreSQL 18 | `docker-compose.yml` |
| Site público | Next.js, TypeScript, Tailwind, shadcn/ui | `frontend/site/` (fase 7) |
| Backoffice | Vite, React, TypeScript, Tailwind, shadcn/ui | `frontend/backoffice/` (fase 6) |

A documentação do projeto está em [`docs/`](docs/): requisitos, base de dados, segurança, componentes e plano de implementação.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://docs.docker.com/engine/install/) com o plugin Compose

## Arrancar em desenvolvimento

### 1. Base de dados

```bash
cp .env.example .env      # e trocar todas as passwords no .env
docker compose up -d --wait
```

Na primeira vez, o script `docker/postgres/init/01-utilizadores.sh` cria a base de dados `portfolio` e três utilizadores:

| Utilizador | Permissões | Usado por |
| --- | --- | --- |
| `portfolio_migracoes` | Cria e altera tabelas | Migrações, só no deploy |
| `portfolio_escrita` | `SELECT`, `INSERT`, `UPDATE`, `DELETE` | Backoffice |
| `portfolio_leitura` | Só `SELECT` | Site público |

Para confirmar as permissões: `./docker/postgres/verificar-permissoes.sh`.

O script só corre com a pasta de dados vazia. Para recomeçar do zero: `docker compose down -v` (apaga todos os dados).

### 2. Segredos da API

As connection strings ficam em `dotnet user-secrets`, nunca no `appsettings.json`. Usar as mesmas passwords do `.env`:

```bash
cd backend/src/Portfolio.Api
dotnet user-secrets set "ConnectionStrings:Leitura"   "Host=localhost;Database=portfolio;Username=portfolio_leitura;Password=..."
dotnet user-secrets set "ConnectionStrings:Escrita"   "Host=localhost;Database=portfolio;Username=portfolio_escrita;Password=..."
dotnet user-secrets set "ConnectionStrings:Migracoes" "Host=localhost;Database=portfolio;Username=portfolio_migracoes;Password=..."
```

### 3. Migrações

Criam as tabelas, com o utilizador `portfolio_migracoes`:

```bash
cd backend
dotnet run --project src/Portfolio.Api -- migrar
```

Depois de alterar uma entidade ou a configuração de uma tabela, gera-se uma nova migração:

```bash
cd backend
dotnet tool restore
dotnet ef migrations add NomeDaAlteracao \
  --project src/Portfolio.Infrastructure --startup-project src/Portfolio.Infrastructure \
  --context BackofficeDbContext --output-dir Dados/Migracoes
```

### 4. API

```bash
cd backend
dotnet run --project src/Portfolio.Api
```

A API corre em `https://localhost:7003`, com o certificado de desenvolvimento do .NET (`dotnet dev-certs https`; para o browser confiar nele, `dotnet dev-certs https --trust`). Corre em HTTPS também em desenvolvimento porque o token CSRF e o cookie de sessão só circulam por HTTPS.

A página para testar os endpoints fica em `https://localhost:7003/scalar` (só em desenvolvimento).

### 5. Conta do backoffice

Não há registo pela internet: a conta cria-se uma única vez, por comando, e a password é pedida sem aparecer no ecrã.

```bash
cd backend
dotnet run --project src/Portfolio.Api -- criar-admin
```

No primeiro login, o backoffice pede para configurar o 2FA (QR code para o Google Authenticator, Authy ou Microsoft Authenticator) e mostra 10 códigos de recuperação, uma única vez.

Password esquecida ou telemóvel do 2FA perdido:

```bash
dotnet run --project src/Portfolio.Api -- repor-password
```

Define uma password nova, desativa o 2FA (configura-se de novo no login seguinte), apaga os códigos de recuperação e termina todas as sessões.

O segredo do 2FA fica cifrado com chaves da Data Protection API, fora da base de dados. Em desenvolvimento ficam em `~/.aspnet/DataProtection-Keys`; em produção, numa pasta definida em `DataProtection__PastaChaves`. Se as chaves se perderem, as sessões terminam e o 2FA tem de ser reposto com `repor-password`.

Os valores do login (bloqueio, sessão, rate limit) estão na secção `Autenticacao` do `appsettings.json`.

### 6. Imagens e timeline do GitHub

As imagens dos projetos ficam em `backend/src/Portfolio.Api/armazenamento/imagens` (fora do Git), ou na pasta definida em `Imagens__Pasta`.

A timeline de cada projeto são os commits do seu repositório no GitHub, lidos pela API e guardados 10 minutos em cache. Sem token, o GitHub aceita 60 pedidos por hora; com um token (*fine-grained*, só leitura de conteúdos) passa a 5000:

```bash
cd backend/src/Portfolio.Api
dotnet user-secrets set "Github:Token" "github_pat_..."
```

Com um token com acesso a repositórios privados, um projeto público mostra as mensagens dos commits desse repositório a toda a gente ([SEG29](docs/03-seguranca.md)).

### 7. Backoffice

Precisa do [Node.js 22](https://nodejs.org/) e da API a correr (passo 4).

```bash
cd frontend/backoffice
npm ci
npm run dev
```

Abre em `http://localhost:5173`. O Vite reencaminha `/api` para a API (`https://localhost:7003`, ou `API_URL`), por isso o browser vê tudo no mesmo endereço e o cookie de sessão funciona como em produção.

As dependências estão com versões exatas e sem scripts de instalação (`.npmrc`). Para acrescentar uma, usar uma versão com pelo menos duas semanas: `npm install --before=AAAA-MM-DD pacote`.

### 8. Site público

Next.js; precisa da API a correr (passo 4).

```bash
cd frontend/site
npm ci
npm run dev
```

Abre em `http://localhost:3000`. As páginas são geradas no servidor Next.js, que pede os dados à API (`API_URL`, por defeito `https://localhost:7003`), e ficam em cache 60 segundos: uma alteração no backoffice aparece no site no máximo um minuto depois.

O Next.js confia no certificado de desenvolvimento do .NET através de `NODE_EXTRA_CA_CERTS`. Exportá-lo uma vez (só o certificado público):

```bash
mkdir -p ~/.aspnet/https
dotnet dev-certs https -ep ~/.aspnet/https/portfolio-dev.pem --format PEM
```

Outras variáveis em `frontend/site/.env.example` (`SITE_URL` para os links absolutos das pré-visualizações no LinkedIn e WhatsApp, `SITE_NOME`).

### Testes

```bash
cd backend
dotnet test
```

Os testes da base de dados arrancam um PostgreSQL num contentor (Testcontainers), com o mesmo script de arranque do `docker-compose`. Sem Docker, são ignorados localmente; no CI falham.

Backoffice (Vitest, com a API simulada):

```bash
cd frontend/backoffice
npm test                # ou npm run test:coverage
npm run lint && npm run typecheck
```

## CI

Os workflows estão em `.github/workflows/` e correm em cada pull request e em cada push para `main`, `stage` e `dev`:

| Workflow | Verifica | Falha quando |
| --- | --- | --- |
| Backend | Build, migrações em dia, testes com cobertura, permissões da base de dados | Cobertura de linhas abaixo de 80% |
| Backoffice | ESLint, tipos, testes com cobertura, build, `npm audit` das dependências de produção | Cobertura de linhas abaixo de 80%, ou vulnerabilidade alta |
| Complexidade | Complexidade ciclomática (lizard), em C#, TypeScript e TSX | Um método ou função passa de 10 |
| Sintaxe | `dotnet format`, actionlint, shellcheck | Erros de formatação, nos workflows ou nos scripts |
| Segurança | CodeQL, gitleaks, dependency review, zizmor | Vulnerabilidade, segredo ou dependência vulnerável |
| Trivy | Repositório e imagens Docker | Vulnerabilidade HIGH ou CRITICAL com correção |
| Mutações | Stryker.NET (ao domingo ou à mão) | Por agora só informa |

### CI local

Os mesmos workflows correm localmente com o [act](https://nektosact.com/installation/), sobre o último commit e numa cópia limpa do repositório:

```bash
./ci.sh                    # todos, exceto mutações
./ci.sh backend sintaxe    # só esses
./ci.sh mutacoes           # mutation testing
```

Os relatórios ficam em `.ci-local/` (a cobertura em `.ci-local/cobertura/index.html`). Localmente ficam de fora o CodeQL, o dependency review e o envio de resultados para o GitHub.

Para o CI correr antes de cada push (e o cancelar se falhar), uma vez por clone:

```bash
git config core.hooksPath .githooks
```

Numa emergência: `git push --no-verify`.
