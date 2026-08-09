# ClerkSuite deploy — DS225+ (Option B: build on Mac → load on NAS)

Concrete path for `mr-storage` using Tailscale SSH. Image is built on the Mac;
the NAS only loads the tarball and runs compose (no SDK build on Synology).

**Cadence:** do **not** deploy on every change. Develop on Mac; use this runbook for
infrequent milestone / clerk-acceptance pushes. Capability fence:
[RESOURCE-NOTES.md](./RESOURCE-NOTES.md) (6 GB RAM confirmed; shared with TIKR; port 8082).

## RAM

- **Installed:** ~6 GB on DS225+ (`mr-storage`) — required for two concurrent Blazor Interactive Server circuits while TIKR/mail also run.
- Design target for app container: **≤ 1536M** RSS (see [RESOURCE-NOTES.md](./RESOURCE-NOTES.md)).
- Monitor with DSM Resource Monitor during clerk acceptance (T7.1).

## Prerequisites (once)

### Mac

- Docker Desktop running
- Tailscale connected (`ssh mr-storage` works)
- Syncfusion license in Keychain (`SYNCFUSION_LICENSE_KEY` / `SYNCFUSION`)

### NAS

- Container Manager installed
- Docs share:

  ```bash
  ssh mr-storage
  sudo mkdir -p /volume1/apartments/docs/{leases,templates,uploads,appliances}
  ```

- Optional verify from Mac repo root:

  ```bash
  ./scripts/verify-documents-mount.sh nas
  ```

## One-liner deploy (preferred)

From repo root on Mac:

```bash
./scripts/deploy-to-nas.sh
```

What it does:

1. `docker build` → `clerksuite:<short-sha>` + `clerksuite:latest`
2. `docker save | gzip` → scp to NAS `/tmp`
3. Installs [docker-compose.yml](./docker-compose.yml) under `/volume1/docker/clerksuite`
4. Writes `.env` from Keychain (`chmod 600`; never printed)
5. `docker load` + `docker compose up -d --force-recreate`

Options: `--skip-build`, `--skip-env`. Host override: `NAS_SSH_HOST=mr-storage`.

## Manual runbook (same sequence)

### 1. Build on Mac

```bash
export IMAGE_TAG="clerksuite:$(git rev-parse --short HEAD)"
docker build -f deploy/Dockerfile -t "$IMAGE_TAG" -t clerksuite:latest .
```

### 2. Transfer

```bash
docker save clerksuite:latest | gzip > /tmp/clerksuite-latest.tar.gz
scp /tmp/clerksuite-latest.tar.gz mr-storage:/tmp/
scp deploy/synology/docker-compose.yml mr-storage:/tmp/clerksuite-docker-compose.yml
```

### 3. NAS project + secrets

```bash
ssh mr-storage
sudo mkdir -p /volume1/docker/clerksuite
sudo mv /tmp/clerksuite-docker-compose.yml /volume1/docker/clerksuite/docker-compose.yml
sudo nano /volume1/docker/clerksuite/.env   # see .env.example
sudo chmod 600 /volume1/docker/clerksuite/.env
```

`.env` keys: `DOCUMENTS_HOST_PATH`, `SYNCFUSION_LICENSE_KEY`, `PaymentPortalUrl`.

### 4. Load + start

```bash
gunzip -c /tmp/clerksuite-latest.tar.gz | sudo /usr/local/bin/docker load
cd /volume1/docker/clerksuite
sudo /usr/local/bin/docker compose --env-file .env up -d
sudo /usr/local/bin/docker logs clerksuite --tail 80
```

From Mac: `curl -sI http://mr-storage:8082`
(Host port **8082** — NAS `:8080` is used by `tikr-web`.)

## First-run checklist

- [ ] Container `clerksuite` is Up
- [ ] Logs show no license / startup errors
- [ ] Login works (seeded clerk accounts)
- [ ] Syncfusion renders without watermark
- [ ] Docs writable (`./scripts/verify-documents-mount.sh nas`)
- [ ] SQLite lives in volume `clerksuite-data` (survives image updates)

## Updates / rollback

Updates: re-run `./scripts/deploy-to-nas.sh` (or `--skip-env` after first deploy).

Rollback: load a previous `clerksuite:<sha>` tag, set `image:` in compose, `up -d --force-recreate`.

## Ops

```bash
sudo /usr/local/bin/docker logs -f clerksuite
sudo /usr/local/bin/docker exec -it clerksuite sh
cd /volume1/docker/clerksuite
sudo /usr/local/bin/docker compose --env-file .env stop
sudo /usr/local/bin/docker compose --env-file .env start
```

## Project notes

- **DB** in Docker volume `clerksuite-data` (not SMB).
- **Documents** on `/volume1/apartments/docs` (Hyper Backup friendly).
- **License** only via `.env` / Container Manager env — never git.
- Local Mac compose with build context remains at [deploy/docker-compose.yml](../docker-compose.yml) for `verify-documents-mount.sh local`.

See [SYNCFUSION-SECRETS.md](./SYNCFUSION-SECRETS.md) and [README.md](./README.md).
