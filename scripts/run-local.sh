#!/usr/bin/env bash
# Local ClerkSuite on Mac — always Development; free the default HTTP port first.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PORT="${CLERKSUITE_PORT:-5077}"
URL="http://localhost:${PORT}"
PROJECT="${ROOT}/src/Wiley.Apartments.Web/Wiley.Apartments.Web.csproj"
DOCS_ROOT="${ClerkSuite__DocumentRoot:-${ROOT}/local-docs}"

if [[ ${ASPNETCORE_ENVIRONMENT:-} == "Production" ]]; then
	echo "Refusing to run: ASPNETCORE_ENVIRONMENT=Production is for published/Docker (NAS) only." >&2
	echo "Unset it or use Development. Example: unset ASPNETCORE_ENVIRONMENT" >&2
	exit 1
fi

echo "==> Freeing port ${PORT} (if held by a prior ClerkSuite process)"
if command -v lsof >/dev/null 2>&1; then
	pids="$(lsof -tiTCP:"${PORT}" -sTCP:LISTEN 2>/dev/null || true)"
	if [[ -n ${pids} ]]; then
		# shellcheck disable=SC2086
		kill ${pids} 2>/dev/null || true
		sleep 1
	fi
fi

export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development
export ClerkSuite__DocumentRoot="${DOCS_ROOT}"

echo "==> Building"
dotnet build "${PROJECT}" -v q

echo "==> Starting ClerkSuite at ${URL} (Development)"
echo "    Document root: ${DOCS_ROOT}"
echo "    Ctrl+C to stop. Re-run this script if you see 'address already in use'."
exec dotnet run --project "${PROJECT}" --no-build --no-launch-profile --urls "${URL}"
