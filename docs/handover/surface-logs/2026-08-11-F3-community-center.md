## Surface: F3 `/community-center`
**Date:** 2026-08-11
**Reviewer:** builder (agent)
**Environment:** local `http://localhost:5077` (Development)
**Build / image / commit:** local run-local after Spec 002 + Phase 12

### 1. Arrive
- [x] Nav or deep link reaches page (no 404 / auth loop)
- [x] Title + subtitle make the job obvious
- [x] Primary action visible without scrolling on laptop (Open unit record + tab buttons)

### 2. Format
- [x] Controls match app kit (PageHeader, SfCard, SfButton)
- [x] Theme contrast OK (exercised in dark mode)
- [x] No horizontal overflow at ~1280px (spot check)
- [x] Loading and empty states are real (Loading… + missing-facility guidance) — **fixed** false “not found” flash
- [x] Errors show banner with recovery (`LoadError`)

### 3. Connected
- [x] Reads `IUnitService.GetFacilityAsync()` (live CC unit Number=CC, Vacant)
- [x] N/A create/edit on hub (navigation surface)
- [x] Open unit record → `/units/{id}`
- [x] Cross-links: Renters, Reservations, Inspections, Inventory, Schedule/Payments/Maintenance with `unitId`, Documents
- [x] Domain: facility unit only; schedule filter shows “Facility CC”
- [x] Deposits N/A on hub

### 4. Usable — job story
> “As clerk, I need a single CC home so that I can reach rentals, money, inspections, and inventory without hunting residential pages.”

- [x] Happy path: open hub → Renters (verified) → Schedule?unitId= (verified filter banner) — ~2 clicks
- [x] Validation N/A
- [x] Destructive N/A
- [x] Print N/A

### 5. Bugs
| Sev | Issue                                               | Repro                               | Fix                                      |
| --- | --------------------------------------------------- | ----------------------------------- | ---------------------------------------- |
| S2  | Hub showed “facility not found” while still loading | First paint with `Facility == null` | `_loaded` gate + “Loading…”              |
| S2  | Documents opens global vault                        | Click Documents                     | Helper copy under tabs (entity-type tip) |
| S3  | Layout card shows 0 sq ft for CC                    | Seeded facility                     | Seeder/data residual — not blocking hub  |

### 6. Verdict
- [ ] PASS — D2
- [x] PASS WITH NOTES (non-blocking S2/S3 only)
- [ ] FAIL (open S0/S1)

**Training notes:** Start at CC hub; use Renters then Reservations for bookings; Schedule/Payments/Maintenance buttons already scope to unit CC.
