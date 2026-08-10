# Spec Kit Done — 001-wiley-apartment-v1

**Verdict:** ✅ DONE  
**Feature dir:** `specs/001-wiley-apartment-v1`  
**Checked:** 2026-08-10 · FR-1–FR-7 · plan stack · constitution · organic surfaces (receipts, deposits, dashboard viz, CC hub)  
**Tests:** 120 unit passed

## Summary

ClerkSuite v1 product scope is complete and live on DS225+ (`mr-storage:8082`). Clerk acceptance (**T7.1**, **T7.3**) signed 2026-08-10 after live review with both town clerks (Stephen present). **T7.4** Spec Kit done audit passes. **T7.2** restore-drill confirmation is **unknown / deferred** by product owner and recorded as a non-blocking IT ops residual (not a product FR gap).

## Organic scope (added during development)

| Surface | Spec’d? | Status | Evidence |
| ------- | ------- | ------ | -------- |
| Payment receipts (NV-1) | backlog → yes | complete | `PaymentReceiptService`, `/payments/receipt` |
| Security deposits (NV-4) | backlog → yes | complete | `IsDeposit`, tenant deposit panel |
| Dashboard viz (NV-5/NV-6) | backlog → yes | complete | Home gauges/heatmap/3D, `/reports/rent-pivot` |
| Community Center hub | converge | complete | `/community-center` |
| DocuSign (NV-2) | post-v1 | deferred | tasks.md |
| CC reservation PDF (NV-3) | post-v1 | deferred | tasks.md |

## Gaps

### Critical

_None for product Spec Kit Done._

### Major

_None._

### Other (deferred / unknown)

- **T7.2** (`unknown`, SC-005 / ops) — Live restore drill may or may not have been run by IT; owner unsure. Preflight showed Active Backup + DB/docs paths. **Next:** when convenient, IT fills [BACKUP-RESTORE.md](../../deploy/synology/BACKUP-RESTORE.md) sign-off. Explicitly non-blocking for T7.4 per Stephen 2026-08-10.

## Next actions

1. Optional: confirm T7.2 with IT and tick the BACKUP-RESTORE table.
2. Open / merge PR from `feature/phase2-tenants-t2.1` when ready.
3. Day-to-day: clerks use `http://mr-storage:8082`.
