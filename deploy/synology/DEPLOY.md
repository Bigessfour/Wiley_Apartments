# ClerkSuite deploy notes — DS225+

## RAM

- **Recommended:** 6 GB RAM on DS225+ before two concurrent Blazor Interactive Server circuits.
- Compose limit for app container: **1536M** (see `deploy/docker-compose.yml`).
- Monitor with DSM Resource Monitor during clerk acceptance (T7.1).

## Deploy steps (summary)

1. Copy repo to NAS or build image locally and push/load.
2. Set `SYNCFUSION_LICENSE_KEY` in Container Manager env (never in git).
3. Mount `/volume1/apartments/docs` → `/docs`.
4. Use SQLite default compose unless Postgres override requested.
5. Seed staff accounts via `SeedUsers` config at first run (passwords chosen at deploy).

## Tailscale + SSH

Use Tailscale for secure MacBook → NAS access during Phase 0 verification (T0.3).

See [README.md](./README.md) and [SYNCFUSION-SECRETS.md](./SYNCFUSION-SECRETS.md).
