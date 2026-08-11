## Surface: F9 `/community-center/inventory`
**Date:** 2026-08-11
**Reviewer:** builder (agent)
**Environment:** local `http://localhost:5077`
**Build / image / commit:** live + OnActionComplete CRUD (no UI dupes) + include-zero ValueChange

### 1. Arrive
- [x] Nav `/community-center/inventory` loads
- [x] Title/subtitle clear
- [x] Primary: Add toolbar + Refresh + CC hub

### 2. Format
- [x] SfGrid dialog edit + SfCheckBox include-zero
- [x] Dark theme OK
- [x] EmptyRecordTemplate present
- [x] Errors via banner

### 3. Connected
- [x] `FacilityInventoryService.List/Create/Update/SoftDelete` (facility unit only)
- [x] Include zero quantity filter (reloads on toggle)
- [x] Soft-delete with confirm dialog
- [x] Domain: facility unit required

### 4. Usable — job story
> “As clerk, I need a hall inventory list so that I know chairs/kitchen stock on hand.”

- [x] Happy path: Add “SurfacePass F9 forks” → single row (4 items) — **no UI duplicate**
- [x] Validation: Name required
- [x] Destructive: Delete confirm
- [x] Print N/A

### 5. Bugs
| Sev | Issue                                               | Repro                                        | Fix                                                                |
| --- | --------------------------------------------------- | -------------------------------------------- | ------------------------------------------------------------------ |
| S1  | Add appeared to duplicate rows                      | Save in OnActionBegin then grid also inserts | Persist in **OnActionComplete** + reload/`Refresh` (same as Units) |
| S2  | Prior items soft-deleted (likely deleting UI dupes) | Empty list after reload                      | Restored Large Pots / small pot / Spoons for local DB              |
| S3  | Category is free-text in dialog (not enum dropdown) | Add dialog                                   | Acceptable; values like Other/Chair work                           |

### 6. Verdict
- [ ] PASS — D2
- [x] PASS WITH NOTES (S1 fixed in session; remaining S3)
- [ ] FAIL (open S0/S1)

**Training notes:** Use Add → Save. If a row looks duplicated after an old build, Refresh — only one DB row exists. Uncheck **Include zero quantity** to hide depleted stock (list reloads on toggle).
