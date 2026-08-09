#!/usr/bin/env bash
# Smoke-test ClerkSuite UI via Chrome DevTools MCP prerequisites (app must be running).
set -euo pipefail

BASE="${CLERKSUITE_URL:-http://localhost:5077}"

echo "==> Checking ClerkSuite at ${BASE}"

for path in \
	"/Account/Login" \
	"/_framework/blazor.web.js" \
	"/css/themes/fluent2.min.css" \
	"/css/themes/fluent2-dark.min.css" \
	"/js/clerksuite-theme.js" \
	"/_content/Syncfusion.Blazor.Core/scripts/syncfusion-blazor.min.js"; do
	code="$(curl -s -o /dev/null -w '%{http_code}' "${BASE}${path}")"
	echo "  ${path} -> ${code}"
	if [[ ${code} != "200" ]]; then
		echo "FAIL: ${path} returned ${code}" >&2
		exit 1
	fi
done

echo "PASS: static assets and login page reachable."
echo "Dev login (Development only): clerk@dev.local / Password1!"
