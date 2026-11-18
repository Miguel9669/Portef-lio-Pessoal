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

A página para testar os endpoints fica em `http://localhost:5079/scalar` (só em desenvolvimento).

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

### Testes

```bash
cd backend
dotnet test
```

Os testes da base de dados arrancam um PostgreSQL num contentor (Testcontainers), com o mesmo script de arranque do `docker-compose`. Sem Docker, são ignorados localmente; no CI falham.

## CI

Os workflows estão em `.github/workflows/` e correm em cada pull request e em cada push para `main`, `stage` e `dev`:

| Workflow | Verifica | Falha quando |
| --- | --- | --- |
| Backend | Build, migrações em dia, testes com cobertura, permissões da base de dados | Cobertura de linhas abaixo de 80% |
| Complexidade | Complexidade ciclomática (lizard) | Um método passa de 10 |
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
