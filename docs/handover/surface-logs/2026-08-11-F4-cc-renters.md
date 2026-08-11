## Surface: F4 `/community-center/renters`
**Date:** 2026-08-11
**Reviewer:** builder (agent)
**Environment:** local `http://localhost:5077` (Development)
**Build / image / commit:** live session; empty-state + error-reload fix in source (hot-reload not required for create path)

### 1. Arrive
- [x] Nav / deep link reaches page
- [x] Title + subtitle state CC hirers ≠ residential tenants
- [x] Primary action visible (Add toolbar + CC hub)

### 2. Format
- [x] SfGrid + dialog edit matches kit neighbors
- [x] Dark theme OK
- [x] No overflow at ~1280px
- [x] Empty state: Syncfusion “No records to display” before seed; **EmptyRecordTemplate** added for clerk-specific hint
- [x] Errors: warning banner; reload after failed mutation

### 3. Connected
- [x] `FacilityRenterService` Create/Search (live create `SurfacePass, F4Test`)
- [x] Soft-delete via grid Delete + confirm dialog (service guards future reservations)
- [x] Detail URL `/community-center/renters/{id}` works (opened created id)
- [x] Cross-link: detail → New reservation / All renters
- [x] Domain: row in `FacilityRenters` only — **not** in `Tenants`
- [x] Deposits N/A

### 4. Usable — job story
> “As clerk, I need to add and find Community Center renters so that bookings and agreements have a clean party record.”

- [x] Happy path: Add → fill required fields → Save → row appears → open detail (~6–8 clicks)
- [x] Validation: grid Required on name/phone/email/address + service checks
- [x] Destructive: ShowDeleteConfirmDialog
- [x] Print N/A

### 5. Bugs
| Sev | Issue                                          | Repro                  | Fix                                       |
| --- | ---------------------------------------------- | ---------------------- | ----------------------------------------- |
| S2  | Blank empty grid with no clerk hint            | Open F4 with 0 renters | `EmptyRecordTemplate` + Settings demo tip |
| S2  | Failed mutation left grid stale risk           | Delete/save error      | `LoadAsync()` in catch                    |
| S3  | Sidebar marks both CC hub + CC renters current | On F4/F5 routes        | Nav Match residual                        |

### 6. Verdict
- [ ] PASS — D2
- [x] PASS WITH NOTES (non-blocking S2/S3 only)
- [ ] FAIL (open S0/S1)

**Training notes:** Use **Add** for new hirers; double-click row for detail/reservations. Soft-delete blocked if future Request/Confirmed bookings exist.
