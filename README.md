# Portfólio Pessoal

Portfólio pessoal com um site público e um backoffice para gerir o conteúdo: blocos de texto do site, projetos e a timeline de notas de cada projeto.

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

### Testes

```bash
cd backend
dotnet test
```

Os testes da base de dados arrancam um PostgreSQL num contentor (Testcontainers), com o mesmo script de arranque do `docker-compose`. Sem Docker, são ignorados localmente; no CI falham.
