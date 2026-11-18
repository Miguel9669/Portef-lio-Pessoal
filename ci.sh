#!/usr/bin/env bash
# CI local: corre os workflows de .github/workflows com o act (https://github.com/nektos/act),
# como o GitHub Actions faria num pull request.
#
# Testa o último commit (HEAD), numa cópia limpa do repositório: alterações por commitar,
# o .env e as pastas bin/obj não entram, tal como no GitHub.
#
# Uso:
#   ./ci.sh                     todos os workflows (exceto mutações, que é lento)
#   ./ci.sh backend sintaxe     só esses
#   ./ci.sh mutacoes            mutation testing (Stryker)
#
# Os relatórios (cobertura, Stryker) ficam em .ci-local/.
# Localmente ficam de fora os passos que precisam da API do GitHub
# (CodeQL, dependency review, upload de SARIF e de artefactos).
set -euo pipefail

raiz=$(git rev-parse --show-toplevel)
cd "$raiz"

predefinidos=(backend backoffice complexidade sintaxe seguranca trivy)
workflows=("$@")
[ ${#workflows[@]} -eq 0 ] && workflows=("${predefinidos[@]}")

erro() { echo "ci.sh: $*" >&2; exit 2; }

command -v act >/dev/null || erro "falta o act: https://nektosact.com/installation/"
docker info >/dev/null 2>&1 || erro "sem acesso ao Docker (sudo usermod -aG docker \$USER e voltar a entrar na sessão)"
for w in "${workflows[@]}"; do
  [ -f ".github/workflows/$w.yml" ] || erro "workflow desconhecido: $w"
done

if ! git diff --quiet HEAD || [ -n "$(git ls-files --others --exclude-standard)" ]; then
  echo "Aviso: há alterações por commitar; o CI testa só o último commit ($(git rev-parse --short HEAD))."
fi

base="${XDG_CACHE_HOME:-$HOME/.cache}/portfolio-ci"
mkdir -p "$base" .ci-local
trabalho=$(mktemp -d "$base/execucao.XXXXXX")

# shellcheck disable=SC2329 # chamada pelo trap
limpar() {
  # Os contentores correm como root, por isso os ficheiros só se apagam com o Docker.
  docker run --rm -v "$trabalho:/t" alpine:3.22 rm -rf /t/copias >/dev/null 2>&1 || true
  rm -rf "$trabalho"
}
trap limpar EXIT

# "act": true faz os workflows saltarem os passos que só funcionam no GitHub.
echo '{"act": true, "pull_request": {"head": {"ref": "local"}, "base": {"ref": "dev"}}}' > "$trabalho/pull_request.json"
echo '{"act": true}' > "$trabalho/workflow_dispatch.json"

declare -A resultado
falhou=0

for w in "${workflows[@]}"; do
  evento=pull_request
  [ "$w" = mutacoes ] && evento=workflow_dispatch

  # Cópia limpa por workflow, para um não ver o que o outro deixou (bin/, obj/, relatórios).
  copia="$trabalho/copias/$w"
  git clone --quiet --no-local "$raiz" "$copia"

  echo
  echo "════════ $w ════════"
  if act "$evento" \
      --directory "$copia" \
      --workflows ".github/workflows/$w.yml" \
      --eventpath "$trabalho/$evento.json" \
      --bind \
      --env-file /dev/null \
      --env TESTCONTAINERS_HOST_OVERRIDE=localhost; then
    resultado[$w]="ok"
  else
    resultado[$w]="FALHOU"
    falhou=1
  fi

  # Relatórios para consultar depois.
  [ -d "$copia/backend/cobertura" ] && rm -rf .ci-local/cobertura && cp -r "$copia/backend/cobertura" .ci-local/cobertura
  [ -d "$copia/stryker" ] && rm -rf .ci-local/stryker && cp -r "$copia/stryker" .ci-local/stryker
done

# O job base-de-dados não corre no act (o container_name do docker-compose chocaria com
# o PostgreSQL de desenvolvimento); verifica-se aqui as permissões nesse mesmo PostgreSQL.
if [[ " ${workflows[*]} " == *" backend "* ]]; then
  echo
  echo "════════ backend / base-de-dados (PostgreSQL de desenvolvimento) ════════"
  if [ ! -f .env ]; then
    echo "Sem .env: passo ignorado (copiar .env.example para .env e definir as passwords)."
    resultado[base-de-dados]="ignorado"
  elif docker compose up -d --wait && ./docker/postgres/verificar-permissoes.sh; then
    resultado[base-de-dados]="ok"
  else
    resultado[base-de-dados]="FALHOU"
    falhou=1
  fi
fi

echo
echo "════════ Resumo ($(git rev-parse --short HEAD)) ════════"
for w in "${!resultado[@]}"; do
  printf '  %-15s %s\n' "$w" "${resultado[$w]}"
done
[ -d .ci-local/cobertura ] && echo "  Cobertura: .ci-local/cobertura/index.html"
[ -d .ci-local/stryker ] && echo "  Mutações:  .ci-local/stryker/"

exit $falhou
