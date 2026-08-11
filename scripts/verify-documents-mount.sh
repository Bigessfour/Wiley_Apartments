#!/usr/bin/env bash
# T0.3 — verify document volume mount (local Docker or NAS via SSH).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="${ROOT}/deploy/docker-compose.yml"
MODE="${1:-local}"
MARKER=".write-test-$(date +%Y%m%d%H%M%S)"

usage() {
	cat <<'EOF'
Usage: verify-documents-mount.sh [local|nas]

  local  — docker compose write test using ./local-docs (default)
  nas    — SSH to mr-storage, alpine container write test on /volume1/apartments/docs

Exit 0 when read/write succeeds.
EOF
}

verify_local() {
	local docs="${ROOT}/local-docs"
	mkdir -p "${docs}"/{leases,templates,uploads,appliances}

	echo "==> Local Docker compose document mount test"
	cd "${ROOT}"

	if ! command -v docker >/dev/null 2>&1; then
		echo "ERROR: docker not found" >&2
		exit 1
	fi

	export DOCUMENTS_HOST_PATH="${docs}"
	docker compose -f "${COMPOSE_FILE}" run --rm --no-deps --entrypoint sh \
		-e SYNCFUSION_LICENSE_KEY="${SYNCFUSION_LICENSE_KEY:-}" \
		clerksuite -c "echo ok-${MARKER} > /docs/${MARKER} && cat /docs/${MARKER}"

	test -f "${docs}/${MARKER}"
	rm -f "${docs}/${MARKER}"
	echo "PASS: local container wrote and read ${docs}/${MARKER}"
}

verify_nas() {
	local ssh_host="${NAS_SSH_HOST:-mr-storage}"
	local docs="/volume1/apartments/docs"
	local docker="sudo /usr/local/bin/docker"

	echo "==> NAS document mount test via ${ssh_host}"

	# Paths/marker expanded locally into the remote command (constants + generated name).
	# shellcheck disable=SC2029
	ssh "${ssh_host}" "sudo mkdir -p ${docs}/{leases,templates,uploads,appliances}"

	# shellcheck disable=SC2029
	ssh "${ssh_host}" "${docker} run --rm -v ${docs}:/docs alpine:3.20 \
    sh -c 'echo ok-${MARKER} > /docs/${MARKER} && cat /docs/${MARKER}'"

	# shellcheck disable=SC2029
	ssh "${ssh_host}" "test -f ${docs}/${MARKER} && sudo rm -f ${docs}/${MARKER}"
	echo "PASS: NAS container wrote and read ${docs}/${MARKER}"
}

case "${MODE}" in
local) verify_local ;;
nas) verify_nas ;;
-h | --help)
	usage
	exit 0
	;;
*)
	echo "Unknown mode: ${MODE}" >&2
	usage
	exit 1
	;;
esac
