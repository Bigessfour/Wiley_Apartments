# ClerkSuite backup & restore (DS225+)

Covers the two data planes clerks depend on:

| Plane            | Location                                                                 | Typical DSM coverage                                                                                          |
| ---------------- | ------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------- |
| SQLite DB volume | Docker volume `clerksuite-data` → `/data/clerksuite.db` inside container | Hyper Backup / Snapshot Replication of `/volume1/docker` (or the volume path DSM shows for Container Manager) |
| Document vault   | Host `/volume1/apartments/docs`                                          | Hyper Backup / Shared Folder Sync / snapshots on the `apartments` share                                       |

**Do not** back up the DB file over SMB as the live write path. Keep SQLite on the Docker volume; back up that volume (or its host bind path).

---

## Pre-flight (once)

1. Confirm Hyper Backup (or Snapshot Replication) includes:
   - Document share: `/volume1/apartments/docs`
   - App project + volume: `/volume1/docker/clerksuite` **and** the Docker volume backing `clerksuite-data`
2. Note retention (daily + weekly) and last successful job time in DSM.
3. Record who can run restores (town IT / mayor / clerk with DSM access).

---

## Restore drill (acceptance — T7.2)

Run this on a maintenance window. Goal: prove DB + docs come back and FR-2 / FR-5 samples still work.

### A. Snapshot / backup point

1. In DSM, identify a known-good Hyper Backup version (or Btrfs snapshot) that includes both planes above.
2. Stop ClerkSuite so the DB file is quiet:

   ```bash
   ssh mr-storage
   cd /volume1/docker/clerksuite
   sudo /usr/local/bin/docker compose stop
   ```

### B. Restore documents

1. Restore `/volume1/apartments/docs` from Hyper Backup / snapshot to the same path (or restore to a temp folder and `rsync -a` into place).
2. Verify folder layout exists:

   ```bash
   ls /volume1/apartments/docs/{leases,templates,uploads,appliances}
   ```

### C. Restore database volume

1. Restore the Docker volume content that maps to `/data` (file `clerksuite.db` + WAL/SHM if present).
2. Confirm ownership/permissions allow the container user to read/write the file.
3. Start the app:

   ```bash
   cd /volume1/docker/clerksuite
   sudo /usr/local/bin/docker compose up -d
   ```

4. Health check (host port **8082**):

   ```bash
   curl -fsS -o /dev/null -w "%{http_code}\n" http://127.0.0.1:8082/health
   # or from Mac via Tailscale:
   curl -fsS -o /dev/null -w "%{http_code}\n" http://mr-storage:8082/
   ```

### D. Functional smoke (clerk browser)

From a clerk PC on Tailscale / LAN:

1. Sign in → open a known tenant (FR-2 sample).
2. Open Documents → view or download a known PDF (FR-5 sample).
3. Confirm Audit log still lists prior mutations (append-only history should survive DB restore).

### E. Sign-off

| Step                                                       | Date | Operator | Pass |
| ---------------------------------------------------------- | ---- | -------- | ---- |
| Hyper Backup covers DB volume + `/volume1/apartments/docs` |      |          |      |
| Restore docs share                                         |      |          |      |
| Restore `clerksuite.db` volume                             |      |          |      |
| App healthy on :8082                                       |      |          |      |
| FR-2 tenant sample OK                                      |      |          |      |
| FR-5 document sample OK                                    |      |          |      |

**T7.2 Done when:** this table is filled and both FR-2 / FR-5 samples succeed after restore.

---

## Notes

- Production URL: `http://mr-storage:8082` (or reverse-proxy HTTPS). Port **8082** avoids conflict with `tikr-web` on 8080.
- Optional Postgres override is not the default; if ever enabled, back up that DB container/volume instead of SQLite.
- After a failed restore, keep the pre-restore tarball/snapshot until clerks confirm data looks right.
