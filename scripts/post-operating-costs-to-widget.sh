#!/usr/bin/env bash
# Pull OperationalEvent[] from ClerkSuite (Wiley Apartments) and POST to Wiley Widget.
# Does not change Apartments behavior — export is read-only.
#
# Usage:
#   ./scripts/post-operating-costs-to-widget.sh
#   PERIOD=2026-10 ./scripts/post-operating-costs-to-widget.sh
#   APARTMENTS_BASE=http://127.0.0.1:5077 WIDGET_API=http://127.0.0.1:5231 ./scripts/post-operating-costs-to-widget.sh
set -euo pipefail

PERIOD="${PERIOD:-$(date -u +%Y-%m)}"
APARTMENTS_BASE="${APARTMENTS_BASE:-http://127.0.0.1:5077}"
WIDGET_API="${WIDGET_API:-${WILEY_WORKSPACE_API_BASE_ADDRESS:-http://127.0.0.1:5231}}"
APARTMENTS_BASE="${APARTMENTS_BASE%/}"
WIDGET_API="${WIDGET_API%/}"

EXPORT_URL="${APARTMENTS_BASE}/api/exports/shared-contracts/operational-events?period=${PERIOD}"

echo "GET  ${EXPORT_URL}"
PAYLOAD="$(curl -sS -f "${EXPORT_URL}")"
COUNT="$(echo "${PAYLOAD}" | jq 'length')"
echo "  → ${COUNT} OperationalEvent(s)"

if [[ "${COUNT}" -eq 0 ]]; then
  echo "Nothing to POST (no UnitOperatingCost rows in ${PERIOD})."
  exit 0
fi

echo "POST ${WIDGET_API}/api/reference-data/events"
RESP="$(curl -sS -w "\n%{http_code}" -X POST "${WIDGET_API}/api/reference-data/events" \
  -H "Content-Type: application/json" \
  --data-binary "${PAYLOAD}")"
BODY="$(echo "${RESP}" | sed '$d')"
CODE="$(echo "${RESP}" | tail -n1)"
echo "  → HTTP ${CODE} ${BODY}"
[[ "${CODE}" == "202" ]] || exit 1
