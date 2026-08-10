# Phase 7 — Hardening & Handover status

**Feature**: `001-wiley-apartment-v1`  
**Branch**: `feature/phase2-tenants-t2.1`  
**Date**: 2026-08-10

## Offline / CI + NAS

| Check | Result |
| ----- | ------ |
| Unit tests | **120 passed** |
| NAS deploy | `clerksuite:2c60129` / `latest` on **8082** |
| Login HTTP | `http://mr-storage:8082/Account/Login` → **200** |

## Task status

| Task | Status |
| ---- | ------ |
| **T7.1** Dual-clerk E2E acceptance | **Done 2026-08-10** — Clerk A + Clerk B signed [quickstart.md](../../specs/001-wiley-apartment-v1/quickstart.md) after live review with Stephen |
| **T7.2** Backup restore drill | **Open** — IT / DSM Active Backup restore + [BACKUP-RESTORE.md](../../deploy/synology/BACKUP-RESTORE.md) sign-off |
| **T7.3** Clerk quick reference | **Done 2026-08-10** — both clerks signed [clerk-quick-reference.md](../clerk-quick-reference.md) |
| **T7.4** Final Spec Kit done | **Open** — blocked on T7.2 only |

## Remaining blocker

Live restore drill (T7.2) for DB volume + `/volume1/apartments/docs`, then re-run `/speckit-done` (T7.4).
