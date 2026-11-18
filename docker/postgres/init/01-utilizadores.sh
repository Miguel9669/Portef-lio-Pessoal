#!/bin/sh
# Cria a base de dados e os três utilizadores com permissões separadas (SEG01):
#   portfolio_migracoes  dono do esquema; cria e altera tabelas (só no deploy)
#   portfolio_escrita    SELECT, INSERT, UPDATE, DELETE (backoffice)
#   portfolio_leitura    só SELECT (site público)
set -eu

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres \
  -v db="$PORTFOLIO_DB" \
  -v pw_leitura="$PORTFOLIO_LEITURA_PASSWORD" \
  -v pw_escrita="$PORTFOLIO_ESCRITA_PASSWORD" \
  -v pw_migracoes="$PORTFOLIO_MIGRACOES_PASSWORD" <<'SQL'
CREATE ROLE portfolio_migracoes LOGIN PASSWORD :'pw_migracoes';
CREATE ROLE portfolio_escrita   LOGIN PASSWORD :'pw_escrita';
CREATE ROLE portfolio_leitura   LOGIN PASSWORD :'pw_leitura';

CREATE DATABASE :"db" OWNER portfolio_migracoes;
REVOKE ALL ON DATABASE :"db" FROM PUBLIC;
GRANT CONNECT ON DATABASE :"db" TO portfolio_escrita, portfolio_leitura;
SQL

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$PORTFOLIO_DB" <<'SQL'
-- O esquema public passa a pertencer ao utilizador das migrações.
ALTER SCHEMA public OWNER TO portfolio_migracoes;
REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO portfolio_escrita, portfolio_leitura;

-- citext (email sem distinguir maiúsculas). Criada aqui, como superutilizador.
CREATE EXTENSION IF NOT EXISTS citext;

-- Permissões das tabelas que as migrações vierem a criar.
ALTER DEFAULT PRIVILEGES FOR ROLE portfolio_migracoes IN SCHEMA public
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO portfolio_escrita;
ALTER DEFAULT PRIVILEGES FOR ROLE portfolio_migracoes IN SCHEMA public
  GRANT USAGE, SELECT ON SEQUENCES TO portfolio_escrita;
ALTER DEFAULT PRIVILEGES FOR ROLE portfolio_migracoes IN SCHEMA public
  GRANT SELECT ON TABLES TO portfolio_leitura;
SQL
