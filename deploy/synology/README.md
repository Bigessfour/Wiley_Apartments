# Town of Wiley — ClerkSuite on Synology DS225+

## Access

- **Production default:** SQLite single container (`deploy/docker-compose.yml`)
- **Dev / deploy from MacBook:** NAS via **Tailscale + SSH** (G6)
- **Documents:** host path `/volume1/apartments/docs` → container `/docs`
- **Database file:** Docker volume `clerksuite-data` → `/data/clerksuite.db` (not SMB)

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

Set in `.env` (not committed):

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
