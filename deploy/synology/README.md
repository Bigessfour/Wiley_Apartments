# Town of Wiley — ClerkSuite on Synology DS225+

**Capability fence + deploy cadence:** [RESOURCE-NOTES.md](./RESOURCE-NOTES.md) — develop on Mac; push images to NAS only for milestone/acceptance tests.

## Access

- **Backup / restore drill (T7.2):** [BACKUP-RESTORE.md](./BACKUP-RESTORE.md)
- **Production deploy (Option B):** build on Mac → load on NAS — see [DEPLOY.md](./DEPLOY.md)
- **NAS compose (image-based):** [docker-compose.yml](./docker-compose.yml) at `/volume1/docker/clerksuite` (host port **8082**)
- **Local Mac compose (build context):** [`deploy/docker-compose.yml`](../docker-compose.yml) for smoke tests
- **Access path:** MacBook → NAS via **Tailscale + SSH** (`mr-storage`)
- **Documents:** host path `/volume1/apartments/docs` → container `/docs`
- **Database file:** Docker volume `clerksuite-data` → `/data/clerksuite.db` (not SMB)

## Deploy (preferred)

```bash
# From Mac, repo root (Docker Desktop + Tailscale + Keychain license)
./scripts/deploy-to-nas.sh
```

## T0.3 verification

```bash
# Local Docker (Mac)
./scripts/verify-documents-mount.sh local

# NAS via Tailscale + SSH
./scripts/verify-documents-mount.sh nas
```

Manual steps on NAS if needed:

```bash
# From MacBook (Tailscale)
ssh mr-storage

# Ensure share exists
sudo mkdir -p /volume1/apartments/docs/{leases,templates,uploads,appliances}
```

Set in `/volume1/docker/clerksuite/.env` (not committed; see [.env.example](./.env.example)):

```bash
DOCUMENTS_HOST_PATH=/volume1/apartments/docs
SYNCFUSION_LICENSE_KEY=<from DSM secrets / Keychain at deploy>
PaymentPortalUrl=https://www.townofwiley.gov/government/departments/finance/utility-billing
```

## PayStar portal (G3)

Staff deep-link only — configure `ClerkSuite__PaymentPortalUrl` to the town pay-bill entry
(`secure.paystar.io` flow via townofwiley.gov).

## Optional Postgres override

```bash
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.postgres.yml up -d
```

Use only when town IT explicitly opts in (G4).
