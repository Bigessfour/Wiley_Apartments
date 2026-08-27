#!/bin/sh
# Bring ClerkSuite back if it has exited. Skip when MAINTENANCE flag is present
# (touch /volume1/docker/clerksuite/MAINTENANCE before a planned stop).
set -eu
DIR=/volume1/docker/clerksuite
FLAG="${DIR}/MAINTENANCE"
NAME=clerksuite
DOCKER=/usr/local/bin/docker

[ -f "${FLAG}" ] && exit 0

status="$("${DOCKER}" inspect -f '{{.State.Status}}' "${NAME}" 2>/dev/null || echo missing)"
case "${status}" in
exited | dead | missing)
	"${DOCKER}" start "${NAME}" >/dev/null 2>&1 || {
		cd "${DIR}"
		"${DOCKER}" compose --env-file .env up -d
	}
	;;
esac
