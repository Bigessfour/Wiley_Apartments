# Phase 7 — Hardening & Handover status

**Feature**: `001-wiley-apartment-v1`  
**Branch**: `feature/phase2-tenants-t2.1`  
**Date**: 2026-08-10

## Offline / CI evidence (completed 2026-08-10)

| Check | Result |
| ----- | ------ |
| `dotnet build` Web project | Pass |
| Unit tests (`Wiley.Apartments.Tests`) | **106 passed** |
| Integration tests (`Wiley.Apartments.IntegrationTests`) | **13 passed** |
| E2E Playwright (`Wiley.Apartments.E2ETests`) | **7 passed** (login + auth redirect; host uses `ASPNETCORE_ENVIRONMENT=Development`) |
| Facility seeder | Seeds 16 residential + `CC` facility (`IsFacility`) |
| Community Center hub | `/community-center` + sidebar section |

## Task status

| Task | Product / docs | Live NAS dual-clerk Done-when |
| ---- | -------------- | ----------------------------- |
| **T7.1** E2E clerk workflow on NAS (both Windows 11) | Automated pyramid green; Mac/local smoke previously noted | **Open** — sign-off table in [quickstart.md](../../specs/001-wiley-apartment-v1/quickstart.md) |
| **T7.2** Backup restore drill | Runbook complete: [BACKUP-RESTORE.md](../../deploy/synology/BACKUP-RESTORE.md) | **Open** — Hyper Backup restore drill + sign-off table |
| **T7.3** Clerk quick reference | Guide complete: [clerk-quick-reference.md](../clerk-quick-reference.md) (includes Community Center) | **Open** — at least one clerk must sign the review table |
| **T7.4** Final converge / done | Reports: [SPECKIT-CONVERGE.md](./SPECKIT-CONVERGE.md), [SPECKIT-DONE.md](./SPECKIT-DONE.md) | Product **converged**; project **not done** until T7.1–T7.3 human evidence |

## Clerk script (when Windows terminals available)

Follow [quickstart.md](../../specs/001-wiley-apartment-v1/quickstart.md) FR-1 → FR-7 on `http://mr-storage:8082` from **both** clerk PCs, then fill the Sign-off table.

```text
create/edit unit → add tenant → occupancy → lease PDF → payment → document upload
+ schedule item + maintenance WO + dashboard glance + audit row
```

## Blockers outside code

- Dual Windows 11 clerk sessions on live DS225+
- Live Hyper Backup restore drill (IT / DSM access)
- Clerk signature on quick-reference

No additional product FRs are blocked on code for Phases 0–6 + Convergence 8–10 + Community Center hub.
