#!/usr/bin/env bash
# Option B: build ClerkSuite on Mac → load + recreate on Synology via Tailscale SSH.
# Never prints or commits SYNCFUSION_LICENSE_KEY.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
NAS_HOST="${NAS_SSH_HOST:-mr-storage}"
NAS_DIR="${NAS_PROJECT_DIR:-/volume1/docker/clerksuite}"
DOCKER_NAS="sudo /usr/local/bin/docker"
COMPOSE_SRC="${ROOT}/deploy/synology/docker-compose.yml"
IMAGE_NAME="clerksuite"
SHORT_SHA="$(git -C "${ROOT}" rev-parse --short HEAD)"
IMAGE_TAG="${IMAGE_NAME}:${SHORT_SHA}"
ARCHIVE="/tmp/clerksuite-${SHORT_SHA}.tar.gz"
SKIP_BUILD=false
SKIP_ENV=false
# DS225+ is amd64; Apple Silicon Macs default to arm64 without --platform.
PLATFORM="${DOCKER_PLATFORM:-linux/amd64}"

usage() {
  cat <<'EOF'
Usage: deploy-to-nas.sh [options]

Build on Mac, transfer image to Synology (mr-storage), load, and recreate container.

Options:
  --skip-build   Reuse existing clerksuite:latest (still transfer + recreate)
  --skip-env     Do not create/update remote .env (must already exist)
  --help         Show this help

Env:
  NAS_SSH_HOST       SSH host (default: mr-storage)
  NAS_PROJECT_DIR    Compose project on NAS (default: /volume1/docker/clerksuite)
  DOCKER_PLATFORM    Build platform (default: linux/amd64)
EOF
}

read_license_from_keychain() {
  local raw=""
  raw="$(security find-generic-password -w -s 'SYNCFUSION_LICENSE_KEY' -a 'SYNCFUSION' 2>/dev/null || true)"
  if [[ -z "${raw}" ]]; then
    raw="$(security find-generic-password -w -s 'com.townofwiley.clerksuite' -a 'SYNCFUSION_LICENSE_KEY' 2>/dev/null || true)"
  fi
  printf '%s' "${raw}" | tr -d '\r\n'
}

# Synology OpenSSH often rejects the SFTP subsystem; prefer legacy SCP (-O) then ssh pipe.
nas_upload() {
  local src="$1"
  local remote_path="$2"
  if scp -O "${src}" "${NAS_HOST}:${remote_path}" 2>/dev/null; then
    return 0
  fi
  echo "    scp -O failed; falling back to ssh pipe for ${remote_path}"
  ssh "${NAS_HOST}" "cat > $(printf %q "${remote_path}")" <"${src}"
}

nas_ssh() {
  ssh "${NAS_HOST}" "$@"
}

for arg in "$@"; do
  case "${arg}" in
    --skip-build) SKIP_BUILD=true ;;
    --skip-env) SKIP_ENV=true ;;
    --help|-h) usage; exit 0 ;;
    *)
      echo "Unknown option: ${arg}" >&2
      usage
      exit 1
      ;;
  esac
done

if [[ ! -f "${COMPOSE_SRC}" ]]; then
  echo "ERROR: missing ${COMPOSE_SRC}" >&2
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "ERROR: docker not found (start Docker Desktop)" >&2
  exit 1
fi

if ! nas_ssh -o BatchMode=yes -o ConnectTimeout=10 'true' 2>/dev/null; then
  echo "ERROR: cannot SSH to ${NAS_HOST} (check Tailscale)" >&2
  exit 1
fi

echo "==> Target ${NAS_HOST}:${NAS_DIR} (image ${IMAGE_TAG})"

if [[ "${SKIP_BUILD}" == false ]]; then
  echo "==> Building ${IMAGE_TAG} and ${IMAGE_NAME}:latest (${PLATFORM})"
  docker build \
    --platform "${PLATFORM}" \
    -f "${ROOT}/deploy/Dockerfile" \
    -t "${IMAGE_TAG}" \
    -t "${IMAGE_NAME}:latest" \
    "${ROOT}"
else
  echo "==> Skipping build (using local ${IMAGE_NAME}:latest)"
  if ! docker image inspect "${IMAGE_NAME}:latest" >/dev/null 2>&1; then
    echo "ERROR: ${IMAGE_NAME}:latest not found locally" >&2
    exit 1
  fi
  local_arch="$(docker image inspect "${IMAGE_NAME}:latest" --format '{{.Architecture}}')"
  if [[ "${local_arch}" != "amd64" ]]; then
    echo "ERROR: ${IMAGE_NAME}:latest is ${local_arch}; NAS needs amd64. Re-run without --skip-build." >&2
    exit 1
  fi
fi

echo "==> Saving image archive ${ARCHIVE}"
docker save "${IMAGE_NAME}:latest" | gzip >"${ARCHIVE}"

echo "==> Uploading archive + compose to ${NAS_HOST}"
nas_upload "${ARCHIVE}" "/tmp/clerksuite-latest.tar.gz"
nas_upload "${COMPOSE_SRC}" "/tmp/clerksuite-docker-compose.yml"

echo "==> Preparing project directory on NAS"
nas_ssh "sudo mkdir -p '${NAS_DIR}' \
  /volume1/apartments/docs/leases \
  /volume1/apartments/docs/templates \
  /volume1/apartments/docs/uploads \
  /volume1/apartments/docs/appliances"
nas_ssh "sudo mv /tmp/clerksuite-docker-compose.yml '${NAS_DIR}/docker-compose.yml'"
nas_ssh "sudo chown root:root '${NAS_DIR}/docker-compose.yml'"

if [[ "${SKIP_ENV}" == false ]]; then
  echo "==> Ensuring remote .env from Keychain (values not printed)"
  license="$(read_license_from_keychain)"
  if [[ -z "${license}" ]]; then
    echo "ERROR: Syncfusion license not found in Keychain" >&2
    exit 1
  fi

  env_tmp="$(mktemp)"
  chmod 600 "${env_tmp}"
  cat >"${env_tmp}" <<EOF
DOCUMENTS_HOST_PATH=/volume1/apartments/docs
SYNCFUSION_LICENSE_KEY=${license}
PaymentPortalUrl=https://www.townofwiley.gov/government/departments/finance/utility-billing
EOF
  nas_upload "${env_tmp}" "/tmp/clerksuite.env"
  rm -f "${env_tmp}"
  nas_ssh "sudo mv /tmp/clerksuite.env '${NAS_DIR}/.env' && sudo chmod 600 '${NAS_DIR}/.env' && sudo chown root:root '${NAS_DIR}/.env'"
else
  echo "==> Skipping .env update (--skip-env)"
  if ! nas_ssh "test -f '${NAS_DIR}/.env'"; then
    echo "ERROR: ${NAS_DIR}/.env missing; re-run without --skip-env" >&2
    exit 1
  fi
fi

echo "==> Loading image and recreating container"
nas_ssh "gunzip -c /tmp/clerksuite-latest.tar.gz | ${DOCKER_NAS} load"
nas_ssh "rm -f /tmp/clerksuite-latest.tar.gz"
nas_ssh "${DOCKER_NAS} rm -f clerksuite >/dev/null 2>&1 || true"
nas_ssh "cd '${NAS_DIR}' && ${DOCKER_NAS} compose --env-file .env up -d --force-recreate"
nas_ssh "${DOCKER_NAS} image prune -f >/dev/null || true"
nas_ssh "${DOCKER_NAS} ps --filter name=clerksuite"
echo "--- logs (tail 40) ---"
nas_ssh "${DOCKER_NAS} logs clerksuite --tail 40"

rm -f "${ARCHIVE}"

echo ""
echo "PASS: deploy finished."
echo "  Image:  ${IMAGE_TAG} (+ ${IMAGE_NAME}:latest)"
echo "  Health: curl -sI http://${NAS_HOST}:8082"
echo "  Docs:   ./scripts/verify-documents-mount.sh nas"
