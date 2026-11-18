#!/bin/sh
# Confirma que cada utilizador da base de dados só pode fazer o que deve (SEG01).
# Uso: com o docker compose a correr, ./docker/postgres/verificar-permissoes.sh
set -eu

falhas=0

# $1 utilizador, $2 SQL, $3 "pode" ou "nao-pode"
verificar() {
  if docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -q -U "$1" -d portfolio -c "$2" >/dev/null 2>&1; then
    resultado=pode
  else
    resultado=nao-pode
  fi
  if [ "$resultado" = "$3" ]; then
    echo "ok     $1 $3: $2"
  else
    echo "FALHA  $1 devia $3: $2"
    falhas=$((falhas + 1))
  fi
}

# Tabela de teste criada pelo dono do esquema, como fariam as migrações.
verificar portfolio_migracoes "CREATE TABLE teste_permissoes (id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY, valor text)" pode

verificar portfolio_escrita "INSERT INTO teste_permissoes (valor) VALUES ('a')" pode
verificar portfolio_escrita "UPDATE teste_permissoes SET valor = 'b'" pode
verificar portfolio_escrita "SELECT * FROM teste_permissoes" pode
verificar portfolio_escrita "DELETE FROM teste_permissoes" pode
verificar portfolio_escrita "CREATE TABLE intrusa (id integer)" nao-pode
verificar portfolio_escrita "DROP TABLE teste_permissoes" nao-pode

verificar portfolio_leitura "SELECT * FROM teste_permissoes" pode
verificar portfolio_leitura "INSERT INTO teste_permissoes (valor) VALUES ('x')" nao-pode
verificar portfolio_leitura "UPDATE teste_permissoes SET valor = 'x'" nao-pode
verificar portfolio_leitura "DELETE FROM teste_permissoes" nao-pode
verificar portfolio_leitura "CREATE TABLE intrusa (id integer)" nao-pode

verificar portfolio_leitura "SELECT 'a'::citext = 'A'::citext" pode

verificar portfolio_migracoes "DROP TABLE teste_permissoes" pode

if [ "$falhas" -gt 0 ]; then
  echo "$falhas verificação(ões) falharam."
  exit 1
fi
echo "Todas as permissões estão corretas."
