# Incomplete subprocesses of existing functions

Captured 2026-08-26. These are child workflows of features that already exist, not new products.

**Out of this pass (still explicit non-goals):** live DocuSign provider, in-app tenant portal/ACH, Syncfusion DocumentEditor for Office files, multi-property SaaS, public CC booking, clerk-vs-admin RBAC (two clerks already share full access). NAS restore-drill T7.2 and unused `Asset.PhotoPaths` stay as documented: vault attachments on the unit remain the photo path.

| Parent | Child (was missing) | Status |
| --- | --- | --- |
| Left nav | List taller than viewport; CC Documents and Documents both went to `/documents` | **Done.** Sidebar nav scrolls. CC Documents opens `/documents?cc=1` (CC entity types only). |
| Operations calendar | Truncated week labels; no hover tooltip; no reminder field; Request bookings ignored overlap; Work order category incomplete | **Done.** Tooltips + reminder hours; Request+Confirmed hold rooms; Work order creates a real WO when a unit is set. |
| Work orders | No appliance → WO from unit page; complete notes omit asset | **Done.** Unit/asset deep link into New WO; Repair ops-cost note includes appliance. |
| Operating costs / P/L | `Renovation` enum not in clerk dropdowns | **Done.** Renovation category + unit-tagged remodel entry + doughnut/column charts. |
| CC inspections | Unsatisfactory did not spawn a WO | **Done.** Fail inspection → WO on the CC unit, linked to the reservation. |
| CC inventory vs reservation equipment | Equipment was a note; stock qty never moved | **Done.** Confirm holds qty; cancel/complete returns it. |
| Ledger “current” | Facility-renter charges mixed into current residential ledger | **Done.** Current occupancy is residential only; CC money when the CC unit is selected (All occupancy). |
| Assets | `PhotoPaths` unused | Unchanged: keep vault attachments on the asset (already on unit detail). |
| E-sign / DocumentEditor / late fees default / restore drill | Provider stubs and NAS ops | Unchanged: wet-ink + upload remains the signing path. |

Related surfaces: `docs/CLERK-SURFACE-COMPLETION.md`, `docs/handover/NAS-CLERK-SMOKE.md`.
