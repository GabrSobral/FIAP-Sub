#!/usr/bin/env bash
#
# End-to-end walk through the whole solution, across both services and both databases.
# This is also the script to screen-record for the demo video.
#
#   docker compose up --build -d
#   ./scripts/e2e.sh
#
set -Eeuo pipefail

CORE_URL="${CORE_URL:-http://localhost:8080}"
SALES_URL="${SALES_URL:-http://localhost:8081}"
INTERNAL_TOKEN="${INTERNAL_TOKEN:-local-dev-internal-token}"

BOLD=$'\e[1m'; GREEN=$'\e[32m'; RED=$'\e[31m'; DIM=$'\e[2m'; RESET=$'\e[0m'
step=0
failures=0

step() { step=$((step + 1)); printf '\n%s[%02d] %s%s\n' "$BOLD" "$step" "$1" "$RESET"; }
pass() { printf '     %s✓%s %s\n' "$GREEN" "$RESET" "$1"; }
fail() { printf '     %s✗%s %s\n' "$RED" "$RESET" "$1"; failures=$((failures + 1)); }
show() { printf '%s%s%s\n' "$DIM" "$(sed 's/^/     /' <<<"$1")" "$RESET"; }

# Prints the HTTP status on the first line and the body on the rest.
request() {
  local method=$1 url=$2 body=${3:-} auth=${4:-public}
  local -a args=(--silent --show-error --write-out $'\n%{http_code}' --request "$method" "$url")

  [[ $auth == internal ]] && args+=(--header "X-Internal-Token: ${INTERNAL_TOKEN}")
  [[ -n $body ]] && args+=(--header 'Content-Type: application/json' --data "$body")

  local raw
  raw=$(curl "${args[@]}")
  printf '%s\n%s\n' "${raw##*$'\n'}" "${raw%$'\n'*}"
}

# Reads one top-level field out of a JSON body on stdin.
field() { python3 -c 'import json,sys; print(json.load(sys.stdin).get(sys.argv[1]))' "$1"; }
pretty() { python3 -m json.tool 2>/dev/null || cat; }

expect() {
  local label=$1 expected=$2 actual=$3
  if [[ $actual == "$expected" ]]; then pass "$label (HTTP $actual)"; else fail "$label — expected HTTP $expected, got $actual"; fi
}

wait_for() {
  local name=$1 url=$2
  for _ in $(seq 1 60); do
    if curl --fail --silent --max-time 2 "$url" >/dev/null 2>&1; then pass "$name is ready"; return 0; fi
    sleep 2
  done
  fail "$name never became ready at $url"
  return 1
}

printf '%sFIAP SOAT — Fase 4 · end-to-end run%s\n' "$BOLD" "$RESET"
printf '%score: %s   sales: %s%s\n' "$DIM" "$CORE_URL" "$SALES_URL" "$RESET"

step 'Both services are up and their own databases answer'
wait_for 'core service'  "$CORE_URL/health/ready"
wait_for 'sales service' "$SALES_URL/health/ready"

step 'Register a vehicle in the catalogue (core service, core database)'
response=$(request POST "$CORE_URL/api/vehicles" \
  '{"brand":"Fiat","model":"Argo Drive 1.3","year":2023,"color":"Vermelho","price":78900.00}')
status=${response%%$'\n'*}; body=${response#*$'\n'}
expect 'vehicle registered' 201 "$status"
show "$(pretty <<<"$body")"
vehicle_id=$(field id <<<"$body")
pass "vehicle id: $vehicle_id"

step 'A cheaper and a dearer vehicle, so the ordering is visible'
for pair in 'Volkswagen|Gol 1.0|2019|Branco|41500.00' 'Toyota|Corolla XEi|2024|Prata|164900.00'; do
  IFS='|' read -r brand model year color price <<<"$pair"
  response=$(request POST "$CORE_URL/api/vehicles" \
    "{\"brand\":\"$brand\",\"model\":\"$model\",\"year\":$year,\"color\":\"$color\",\"price\":$price}")
  expect "registered $brand $model" 201 "${response%%$'\n'*}"
done

step 'The sales service already has them, pushed over HTTP, ordered cheapest first'
response=$(request GET "$SALES_URL/api/vehicles/for-sale?pageSize=100")
status=${response%%$'\n'*}; body=${response#*$'\n'}
expect 'for-sale listing served from the sales database' 200 "$status"
show "$(python3 -c '
import json, sys
page = json.load(sys.stdin)
for item in page["items"]:
    print("{:>12,.2f} {}  {} {} ({}, {})".format(
        item["price"], item["currency"], item["brand"], item["model"], item["year"], item["status"]))
print("total: {}".format(page["totalCount"]))
' <<<"$body")"
prices=$(python3 -c 'import json,sys; print(" ".join(str(i["price"]) for i in json.load(sys.stdin)["items"]))' <<<"$body")
sorted_prices=$(tr ' ' '\n' <<<"$prices" | sort -g | tr '\n' ' ')
if [[ "$prices " == "$sorted_prices" ]]; then pass 'listing is ordered by price ascending'; else fail "listing is not ordered: $prices"; fi

step 'Buy the vehicle (sales service) — CPF of the buyer'
response=$(request POST "$SALES_URL/api/sales" \
  "{\"vehicleId\":\"$vehicle_id\",\"cpf\":\"529.982.247-25\"}")
status=${response%%$'\n'*}; body=${response#*$'\n'}
expect 'purchase opened' 201 "$status"
show "$(pretty <<<"$body")"
sale_id=$(field id <<<"$body")
payment_code=$(field paymentCode <<<"$body")
pass "sale id: $sale_id    payment code: $payment_code"

step 'The catalogue is holding the vehicle, and it left the for-sale listing'
response=$(request GET "$CORE_URL/api/vehicles/$vehicle_id")
body=${response#*$'\n'}
vehicle_status=$(field status <<<"$body")
[[ $vehicle_status == Reserved ]] && pass 'core says Reserved' || fail "core says $vehicle_status"
response=$(request GET "$SALES_URL/api/vehicles/for-sale?pageSize=100")
if grep -q "$vehicle_id" <<<"${response#*$'\n'}"; then fail 'the vehicle is still on sale'; else pass 'gone from the for-sale listing'; fi

step 'A second buyer is turned away while the payment is pending'
response=$(request POST "$SALES_URL/api/sales" \
  "{\"vehicleId\":\"$vehicle_id\",\"cpf\":\"111.444.777-35\"}")
expect 'second purchase refused' 409 "${response%%$'\n'*}"

step 'The payment provider calls the webhook on the core service — approved'
response=$(request POST "$CORE_URL/api/webhooks/payments" \
  "{\"paymentCode\":\"$payment_code\",\"status\":\"approved\"}")
status=${response%%$'\n'*}; body=${response#*$'\n'}
expect 'webhook accepted' 200 "$status"
show "$(pretty <<<"$body")"

step 'Replaying the same webhook changes nothing (idempotent)'
response=$(request POST "$CORE_URL/api/webhooks/payments" \
  "{\"paymentCode\":\"$payment_code\",\"status\":\"approved\"}")
status=${response%%$'\n'*}; body=${response#*$'\n'}
expect 'replay accepted' 200 "$status"
already=$(field alreadyProcessed <<<"$body")
[[ $already == True ]] && pass 'reported as already processed' || fail "alreadyProcessed was $already"

step 'The sale is closed with a sale date, and the vehicle is in the sold listing'
response=$(request GET "$SALES_URL/api/sales/$sale_id")
body=${response#*$'\n'}
show "$(pretty <<<"$body")"
sale_status=$(field status <<<"$body")
sale_date=$(field saleDate <<<"$body")
[[ $sale_status == Paid ]] && pass 'sale is Paid' || fail "sale is $sale_status"
[[ $sale_date != None ]] && pass "sale date: $sale_date" || fail 'the sale has no sale date'

response=$(request GET "$SALES_URL/api/vehicles/sold?pageSize=100")
status=${response%%$'\n'*}; body=${response#*$'\n'}
expect 'sold listing served' 200 "$status"
if grep -q "$vehicle_id" <<<"$body"; then pass 'the vehicle is in the sold listing'; else fail 'the vehicle is missing from the sold listing'; fi

step 'A cancelled payment puts a vehicle back on sale'
response=$(request POST "$CORE_URL/api/vehicles" \
  '{"brand":"Honda","model":"Civic EXL","year":2022,"color":"Preto","price":142000.00}')
second_vehicle=$(field id <<<"${response#*$'\n'}")
response=$(request POST "$SALES_URL/api/sales" \
  "{\"vehicleId\":\"$second_vehicle\",\"cpf\":\"111.444.777-35\"}")
second_payment=$(field paymentCode <<<"${response#*$'\n'}")
response=$(request POST "$CORE_URL/api/webhooks/payments" \
  "{\"paymentCode\":\"$second_payment\",\"status\":\"cancelled\"}")
expect 'cancellation accepted' 200 "${response%%$'\n'*}"

response=$(request GET "$CORE_URL/api/vehicles/$second_vehicle")
vehicle_status=$(field status <<<"${response#*$'\n'}")
[[ $vehicle_status == Available ]] && pass 'core put the vehicle back to Available' || fail "core says $vehicle_status"

response=$(request GET "$SALES_URL/api/vehicles/for-sale?pageSize=100")
if grep -q "$second_vehicle" <<<"${response#*$'\n'}"; then pass 'back on the for-sale listing'; else fail 'not back on the for-sale listing'; fi

step 'A webhook for an unknown payment code is rejected'
# Fresh code every run: the notification ledger is permanent, so a reused one would come back
# as an already-processed replay instead of a rejection.
unknown_code="PAY-nobody-$(date +%s)-$RANDOM"
response=$(request POST "$CORE_URL/api/webhooks/payments" "{\"paymentCode\":\"$unknown_code\",\"status\":\"approved\"}")
expect 'unknown payment code refused' 404 "${response%%$'\n'*}"

step 'The internal service-to-service routes are not open to the public'
response=$(request POST "$CORE_URL/api/internal/vehicles/$vehicle_id/reserve" '{"paymentCode":"PAY-x"}')
expect 'core internal route requires the shared token' 401 "${response%%$'\n'*}"
response=$(request POST "$SALES_URL/api/internal/payments/result" \
  '{"paymentCode":"PAY-x","status":"approved","vehicleId":"00000000-0000-0000-0000-000000000000","occurredOn":"2026-01-01T00:00:00Z"}')
expect 'sales internal route requires the shared token' 401 "${response%%$'\n'*}"

step 'Editing a sold vehicle is refused'
response=$(request PUT "$CORE_URL/api/vehicles/$vehicle_id" \
  '{"brand":"Fiat","model":"Argo","year":2023,"color":"Azul","price":1000.00}')
expect 'sold vehicle cannot be edited' 409 "${response%%$'\n'*}"

step 'The sales projection can be rebuilt from the catalogue over HTTP'
response=$(request POST "$SALES_URL/api/internal/catalogue/sync?pageSize=100" '' internal)
status=${response%%$'\n'*}; body=${response#*$'\n'}
expect 'catalogue sync ran' 200 "$status"
show "$(pretty <<<"$body")"

printf '\n%s' "$BOLD"
if ((failures == 0)); then
  printf '%sAll %d steps passed.%s\n' "$GREEN" "$step" "$RESET"
else
  printf '%s%d check(s) failed.%s\n' "$RED" "$failures" "$RESET"
  exit 1
fi
