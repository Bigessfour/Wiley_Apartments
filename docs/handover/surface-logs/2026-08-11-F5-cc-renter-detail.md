## Surface: F5 `/community-center/renters/{id}`
**Date:** 2026-08-11  
**Reviewer:** builder (agent)  
**Environment:** local `http://localhost:5077`  
**Build / image / commit:** live + display-time/Notes fix in source

### 1. Arrive
- [x] Deep link `/community-center/renters/{id}` loads
- [x] Title = renter name; subtitle = org or “Community Center renter”
- [x] Primary actions: All renters + New reservation

### 2. Format
- [x] PageHeader + SfButton pattern
- [x] Dark theme OK
- [x] No overflow
- [x] Loading… when null; “Renter not found.” banner for bad id
- [x] Errors via alert banner

### 3. Connected
- [x] `FacilityRenterService.GetByIdAsync` + `ReservationService.ListAsync(facilityRenterId:)`
- [x] N/A edit on detail (edit on F4 grid by design)
- [x] Reservation links use `/community-center/reservations/{id}`
- [x] **New reservation** → `/reservations?renterId=` preselects `SurfacePass, F4Test`
- [x] Soft-deleted / missing → not found (clears renter body)
- [x] Domain: CC-only party; empty reservations copy points to New reservation

### 4. Usable — job story
> “As clerk, I need renter contact + their bookings so that I can start a reservation or open an existing one.”

- [x] Happy path: open from F4 create → review fields → New reservation (prefilled) — ~2–3 clicks
- [x] Validation N/A (read-only)
- [x] Destructive N/A
- [x] Print N/A

### 5. Bugs
| Sev | Issue | Repro | Fix |
|-----|-------|-------|-----|
| S2 | Reservation list used UTC `ToString("u")` | Had bookings | `Clock.ToDisplayTime` |
| S3 | Notes not shown on detail | Open renter | Notes row added |
| S3 | Plain `<ul>` vs SfGrid for few links | With reservations | Acceptable for sparse list |

### 6. Verdict
- [ ] PASS — D2
- [x] PASS WITH NOTES (non-blocking S2/S3 only)
- [ ] FAIL (open S0/S1)

**Training notes:** Edit contact fields on the renters grid (F4). From detail, **New reservation** keeps the renter selected.
