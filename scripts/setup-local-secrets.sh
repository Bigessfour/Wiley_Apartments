#!/usr/bin/env bash
# Sync Keychain secrets into dotnet user-secrets for local dev (never commit values).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WEB_PROJECT="$ROOT/src/Wiley.Apartments.Web/Wiley.Apartments.Web.csproj"

usage() {
  cat <<'EOF'
Usage: setup-local-secrets.sh [--license-only] [--help]

Reads Syncfusion license from macOS Keychain into dotnet user-secrets.
Does not print secret values.

Keychain lookup order:
  1. service SYNCFUSION_LICENSE_KEY / account SYNCFUSION
  2. service com.townofwiley.clerksuite / account SYNCFUSION_LICENSE_KEY
EOF
}

read_license_from_keychain() {
  local raw=""
  raw="$(security find-generic-password -w -s 'SYNCFUSION_LICENSE_KEY' -a 'SYNCFUSION' 2>/dev/null || true)"
  if [[ -z "$raw" ]]; then
    raw="$(security find-generic-password -w -s 'com.townofwiley.clerksuite' -a 'SYNCFUSION_LICENSE_KEY' 2>/dev/null || true)"
  fi
  printf '%s' "$raw" | tr -d '\r\n'
}

LICENSE_ONLY=false
for arg in "$@"; do
  case "$arg" in
    --license-only) LICENSE_ONLY=true ;;
    --help|-h) usage; exit 0 ;;
    *) echo "Unknown option: $arg" >&2; usage; exit 1 ;;
  esac
done

if [[ ! -f "$WEB_PROJECT" ]]; then
  echo "Web project not found: $WEB_PROJECT" >&2
  exit 1
fi

license="$(read_license_from_keychain)"
if [[ -z "$license" ]]; then
  echo "Syncfusion license not found in Keychain." >&2
  exit 1
fi

dotnet user-secrets set "SYNCFUSION_LICENSE_KEY" "$license" --project "$WEB_PROJECT" >/dev/null
echo "SYNCFUSION_LICENSE_KEY synced to user-secrets for Wiley.Apartments.Web."

if [[ "$LICENSE_ONLY" == false ]]; then
  echo "MCP API key: use Keychain bridge or ~/.config/syncfusion/api.key (see READINESS.md §8)."
fi
