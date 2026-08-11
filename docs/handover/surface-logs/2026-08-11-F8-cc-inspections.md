## Surface: F8 `/community-center/inspections`
**Date:** 2026-08-11  
**Reviewer:** builder (agent)  
**Environment:** local `http://localhost:5077`  
**Build / image / commit:** live + EmptyRecordTemplate

### 1. Arrive
- [x] Nav `/community-center/inspections` loads
- [x] Title/subtitle: Pre- and post-rental condition checks
- [x] Primary: **Reservations** + per-row **Reservation**

### 2. Format
- [x] PageHeader + SfGrid
- [x] Dark theme OK
- [x] EmptyRecordTemplate: “record Pre/PostRental on a reservation detail”
- [x] Errors via banner (grid still renders)

### 3. Connected
- [x] `InspectionService.ListRecentAsync(100)` with renter via reservation include
- [x] Local display time for InspectedUtc
- [x] Row **Reservation** → `/community-center/reservations/{id}` (verified Completed SurfacePass booking)
- [x] Header **Reservations** → list
- [x] Create path is on F7 detail (by design — list is cross-booking queue)

### 4. Usable — job story
> “As clerk, I need a queue of recent CC inspections so that I can jump back to the booking.”

- [x] Happy path: open list → see PostRental for SurfacePass → Reservation (~1–2 clicks)
- [x] Validation N/A (read-only list)
- [x] Destructive N/A
- [x] Print N/A (photos on F7)

### 5. Bugs
| Sev | Issue | Repro | Fix |
|-----|-------|-------|-----|
| S3 | No type/date filters on list | Want PostRental-only | Optional; take=100 enough for town |
| S3 | No notes/damage column | See issues at a glance | Open reservation detail |

### 6. Verdict
- [ ] PASS — D2
- [x] PASS WITH NOTES (S3 only)
- [ ] FAIL (open S0/S1)

**Training notes:** Log inspections on the reservation detail. This page is the cross-booking index.
