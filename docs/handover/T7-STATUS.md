# Phase 7 — Hardening & Handover status

**Feature**: `001-wiley-apartment-v1`  
**Branch**: `feature/phase2-tenants-t2.1` @ `2c60129`  
**Date**: 2026-08-10 (updated after NAS deploy)

## Offline / CI evidence

| Check | Result |
| ----- | ------ |
| `dotnet build` Web project | Pass |
| Unit tests (`Wiley.Apartments.Tests`) | **120 passed** (2026-08-10) |
| Integration / E2E | Previously green on branch (13 + 7) |
| Local Mac dashboard / receipt smoke | Pass (Chrome) after Syncfusion API fixes |
| NAS deploy `clerksuite:2c60129` | **Pass** — `./scripts/deploy-to-nas.sh --skip-env` → container Up on **8082** |
| NAS login HTTP | `http://mr-storage:8082/Account/Login` → **200** |
| Receipt template on DocumentRoot | `/volume1/apartments/docs/templates/Wiley_Payment_Receipt_Template.pdf` |

## Task status

| Task | Agent / docs prep | Live Done-when (human) |
| ---- | ----------------- | ---------------------- |
| **T7.1** E2E clerk workflow on NAS (both Windows 11) | Image live; login page healthy; Mac Tailscale reachability proven | **Open** — both clerks must complete [quickstart.md](../../specs/001-wiley-apartment-v1/quickstart.md) sign-off |
| **T7.2** Backup restore drill | Runbook updated: Active Backup package present on DS225+; Docker volume `clerksuite_clerksuite-data` + `/volume1/apartments/docs` mapped | **Open** — IT must run restore drill + sign [BACKUP-RESTORE.md](../../deploy/synology/BACKUP-RESTORE.md) |
| **T7.3** Clerk quick reference | Guide + screenshots under [screenshots/](./screenshots/) | **Open** — ≥1 clerk signs review table in [clerk-quick-reference.md](../clerk-quick-reference.md) |
| **T7.4** Final converge / done | Product converged; Spec Kit done still blocked on T7.1–T7.3 human evidence | Re-run after clerk/IT sign-off |

## Clerk script (Windows terminals)

Open `http://mr-storage:8082` from **both** clerk PCs, then:

```text
create/edit unit → add tenant → occupancy → lease PDF → payment → receipt → document upload
+ schedule item + maintenance WO + dashboard glance + audit row
```

Fill the Sign-off table in quickstart.md.

## Blockers outside code

- Dual Windows 11 clerk sessions signing T7.1
- Live restore drill (IT / DSM — Active Backup or Hyper Backup)
- Clerk signature on quick-reference (T7.3)

No additional product FRs are blocked on code for Phases 0–6 + Convergence + dashboard viz + receipts/deposits.
