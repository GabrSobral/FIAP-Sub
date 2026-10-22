#!/usr/bin/env bash
#
# Publishes this service to whatever cluster kubectl points at, and proves it answers.
# The CI deploy job runs it after every merge into main; it works the same against a local
# kind cluster, which is how the demo video shows the infrastructure running:
#
#   kind create cluster --name fiap-sub
#   docker build -t fiapsub-sales:local .
#   kind load docker-image fiapsub-sales:local --name fiap-sub
#   IMAGE=fiapsub-sales:local ./scripts/deploy.sh
#
# Environment:
#   IMAGE                 image to roll out (required)
#   DB_PASSWORD           database password; generated on first deploy when absent
#   INTERNAL_TOKEN        shared /api/internal token, the same value in both services;
#                         generated on first deploy when absent
#   REGISTRY_PULL_TOKEN   optional token for a private registry (e.g. a GHCR read:packages PAT)
#   REGISTRY_SERVER       registry for REGISTRY_PULL_TOKEN (default ghcr.io)
#   REGISTRY_USER         user for REGISTRY_PULL_TOKEN
set -Eeuo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

: "${IMAGE:?set IMAGE to the image to deploy}"

NAMESPACE=fiap-sub
APP=sales-api
DB=sales-db
DB_NAME=fiapsub_sales
DB_USER=fiapsub
SMOKE_PORT=18081

log() { printf '\n\e[1m==> %s\e[0m\n' "$1"; }

# Secrets never live in the repository. They are created from the environment, or generated the
# first time and then left alone: regenerating the database password on every deploy would lock
# the API out of a volume that Postgres already initialised with the old one.
ensure_secret() {
  local name=$1 provided=$2
  shift 2

  if [[ $provided == false ]] && kubectl -n "$NAMESPACE" get secret "$name" >/dev/null 2>&1; then
    echo "secret/$name already exists, keeping it"
    return
  fi

  kubectl -n "$NAMESPACE" create secret generic "$name" "$@" --dry-run=client -o yaml \
    | kubectl apply -f -
}

current_secret_value() {
  kubectl -n "$NAMESPACE" get secret "$1" -o "jsonpath={.data.$2}" 2>/dev/null | base64 --decode || true
}

log "Namespace and configuration"
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml

log "Secrets"
db_provided=false; token_provided=false
[[ -n ${DB_PASSWORD:-} ]] && db_provided=true
[[ -n ${INTERNAL_TOKEN:-} ]] && token_provided=true

DB_PASSWORD=${DB_PASSWORD:-$(current_secret_value "$DB-credentials" POSTGRES_PASSWORD)}
DB_PASSWORD=${DB_PASSWORD:-$(openssl rand -hex 24)}
INTERNAL_TOKEN=${INTERNAL_TOKEN:-$(current_secret_value "$APP-secrets" InternalApi__Token)}
INTERNAL_TOKEN=${INTERNAL_TOKEN:-$(openssl rand -hex 32)}

ensure_secret "$DB-credentials" "$db_provided" \
  --from-literal=POSTGRES_DB="$DB_NAME" \
  --from-literal=POSTGRES_USER="$DB_USER" \
  --from-literal=POSTGRES_PASSWORD="$DB_PASSWORD"

app_provided=false
[[ $db_provided == true || $token_provided == true ]] && app_provided=true

ensure_secret "$APP-secrets" "$app_provided" \
  --from-literal=Database__ConnectionString="Host=$DB;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD" \
  --from-literal=InternalApi__Token="$INTERNAL_TOKEN" \
  --from-literal=CatalogueService__InternalToken="$INTERNAL_TOKEN"

if [[ -n ${REGISTRY_PULL_TOKEN:-} ]]; then
  kubectl -n "$NAMESPACE" create secret docker-registry registry-pull \
    --docker-server="${REGISTRY_SERVER:-ghcr.io}" \
    --docker-username="${REGISTRY_USER:?set REGISTRY_USER with REGISTRY_PULL_TOKEN}" \
    --docker-password="$REGISTRY_PULL_TOKEN" \
    --dry-run=client -o yaml | kubectl apply -f -
  kubectl -n "$NAMESPACE" patch serviceaccount default \
    -p '{"imagePullSecrets":[{"name":"registry-pull"}]}'
fi

log "Database"
kubectl apply -f k8s/postgres.yaml
kubectl -n "$NAMESPACE" rollout status "statefulset/$DB" --timeout=5m

log "API: $IMAGE"
kubectl apply -f k8s/service.yaml
# The manifest names a placeholder image; render the real one in, so a rollout never starts
# pods on an image that does not exist.
sed "s#^\( *image: \).*/fiapsub-[a-z]*:.*#\1$IMAGE#" k8s/deployment.yaml | kubectl apply -f -
kubectl -n "$NAMESPACE" rollout status "deployment/$APP" --timeout=5m
kubectl apply -f k8s/hpa.yaml
kubectl apply -f k8s/ingress.yaml

log "Smoke test through the Service"
kubectl -n "$NAMESPACE" port-forward "service/$APP" "$SMOKE_PORT:8080" >/dev/null &
forward=$!
trap 'kill "$forward" 2>/dev/null || true' EXIT

base="http://localhost:$SMOKE_PORT"
for _ in $(seq 1 30); do
  curl --silent --fail "$base/health/live" >/dev/null && break
  sleep 1
done

curl --silent --show-error --fail "$base/health/ready"
echo

# Readiness already proved the database is reachable; this proves the public API answers.
curl --silent --show-error --fail "$base/api/vehicles/for-sale?page=1&pageSize=1" >/dev/null
echo "GET /api/vehicles/for-sale answered"

kubectl -n "$NAMESPACE" get statefulset,deployment,service,hpa,ingress,pods

log "Deployed $IMAGE"
