# ClerkSuite backup & restore (DS225+)

Covers the two data planes clerks depend on:

| Plane | Location | Typical DSM coverage |
| ----- | -------- | -------------------- |
| Postgres database | Host `127.0.0.1:5433`, database `clerksuite`, role `clerksuite_app`. The container uses host networking. | Hyper Backup or a `pg_dump` of that database. Do not treat the Docker volume SQLite file as the live database. |
| Document vault | Host `/volume1/apartments/docs` | Active Backup / Hyper Backup / Shared Folder Sync / snapshots on the `apartments` share |

The Docker volume `clerksuite-data` still holds Data Protection keys and an offline SQLite file. It is not the clerk database. Do not back up a live database file over SMB.

### Agent preflight (2026-08-10)

| Check | Result |
| ----- | ------ |
| Document share present | `/volume1/apartments/docs` with `leases`, `templates`, `uploads`, `appliances`, `tenants` |
| Live database | Postgres `clerksuite` on `127.0.0.1:5433` |
| DSM backup package | `ActiveBackup` package present on Mr_Storage (Hyper Backup package name not listed) |
| Live restore drill | **Not signed.** Leave the table in section E blank until the special audit. |

---

## Pre-flight (once)

1. Confirm Active Backup and/or Hyper Backup (or Snapshot Replication) includes:
   - Document share: `/volume1/apartments/docs`
   - Postgres data for database `clerksuite` on `127.0.0.1:5433` (dump or the Postgres data directory)
2. Note retention (daily + weekly) and last successful job time in DSM.
3. Record who can run restores (town IT / mayor / clerk with DSM access).

---

## Restore drill (acceptance — T7.2)

Run this on a maintenance window. Goal: prove DB + docs come back and FR-2 / FR-5 samples still work.

### A. Snapshot / backup point

1. In DSM, identify a known-good Hyper Backup version (or Btrfs snapshot) that includes both planes above.
2. Stop ClerkSuite so Postgres is quiet:

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

### C. Restore Postgres

1. Restore database `clerksuite` on `127.0.0.1:5433` from the dump or snapshot. Role `clerksuite_app` must be able to connect.
2. Do not restore `clerksuite.db` from the Docker volume over this database. That file is an offline leftover.
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
| Hyper Backup / Active Backup covers DB volume + `/volume1/apartments/docs` |      |          |      |
| Restore docs share                                         |      |          |      |
| Restore Postgres `clerksuite` on `127.0.0.1:5433`          |      |          |      |
| App healthy on :8082                                       |      |          |      |
| FR-2 tenant sample OK                                      |      |          |      |
| FR-5 document sample OK                                    |      |          |      |

**T7.2 Done when:** this table is filled and both FR-2 / FR-5 samples succeed after restore.

---

## Notes

- Clerk URL: `https://mr-storage.tail57aa13.ts.net:8443`. Port **8082** is the container listener and avoids `tikr-web` on 8080.
- After a failed restore, keep the pre-restore dump or snapshot until clerks confirm data looks right.
