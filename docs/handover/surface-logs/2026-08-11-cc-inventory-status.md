# Surface inventory status — Spec 002 CC refresh

**Date:** 2026-08-11
**Skill:** `project-completion` (D2 Daily-ops Ready)
**Environment:** code connectivity review (no live Mac/NAS clerk session this pass)
**Commit context:** Spec Kit `002-community-center-facility` + Phase 12 convergence on `master`

## Done level

Target remains **D2**. Spec 002 is **D1 Spec Done**. Prior residential surfaces stay PASS WITH NOTES; CC expanded surfaces must re-enter the D2 queue.

## Route ↔ inventory reconciliation

| Route (from `@page` / nav)            | Inventory ID | Prior status    | Status after sync                                      |
| ------------------------------------- | ------------ | --------------- | ------------------------------------------------------ |
| `/Account/Login`                      | A1           | PASS WITH NOTES | unchanged                                              |
| Shell                                 | A2           | PASS WITH NOTES | unchanged                                              |
| `/Error`                              | A3           | PASS WITH NOTES | unchanged                                              |
| `/`                                   | B1           | PASS WITH NOTES | unchanged                                              |
| `/reports` + H*                       | B2/H1–H8     | PASS WITH NOTES | unchanged                                              |
| `/units`, `/units/{id}`               | C1–C2        | PASS WITH NOTES | unchanged                                              |
| `/tenants`, `/tenants/{id}`           | C3–C4        | PASS WITH NOTES | unchanged                                              |
| `/leases*`                            | D1–D3        | PASS WITH NOTES | unchanged                                              |
| `/payments*`                          | E1–E2        | PASS WITH NOTES | unchanged                                              |
| `/schedule`                           | F1           | PASS WITH NOTES | unchanged (FacilityRental CSS added)                   |
| `/maintenance`                        | F2           | PASS WITH NOTES | unchanged + Completer/date/reservation link (re-smoke) |
| `/community-center`                   | F3           | was checked     | **Unchecked** (hub rebuilt for 002)                    |
| `/community-center/renters`           | F4           | missing         | **Unchecked** (new)                                    |
| `/community-center/renters/{id}`      | F5           | missing         | **Unchecked** (new)                                    |
| `/community-center/reservations`      | F6           | missing         | **Unchecked** (new)                                    |
| `/community-center/reservations/{id}` | F7           | missing         | **Unchecked** (new)                                    |
| `/community-center/inspections`       | F8           | missing         | **Unchecked** (new)                                    |
| `/community-center/inventory`         | F9           | missing         | **Unchecked** (new)                                    |
| `/documents`, `/audit`                | G1–G2        | PASS WITH NOTES | unchanged (CC entity types added)                      |
| `/settings`                           | I1           | PASS WITH NOTES | unchanged                                              |
| `/Account/Logout`                     | (shell)      | covered by A2   | n/a                                                    |

## Out of D2 scope (updated)

- Live DocuSign
- Tenant portal / ACH
- Multi-property SaaS
- Public CC booking portal
- ~~CC reservation PDF (post-v1)~~ — **removed**; shipped in Spec 002

## Code-connectivity notes (F3–F9) — not live PASS D2

| ID  | Arrive / services                                                             | Connected graph                                                        | Open notes (not S0/S1 from code alone)                         |
| --- | ----------------------------------------------------------------------------- | ---------------------------------------------------------------------- | -------------------------------------------------------------- |
| F3  | Hub + SfCard + deep links                                                     | Links to F4–F9, schedule/payments/maintenance with `unitId`, documents | Documents deep link is global vault (S2: no CC path prefilter) |
| F4  | SfGrid CRUD → `FacilityRenterService`                                         | Double-click → F5; soft-delete guard for future bookings               | Empty-state template optional (S3)                             |
| F5  | Read-only detail + reservation list                                           | Links → F7; New reservation → F6 `?renterId=`                          | Edit stays on F4 grid (OK)                                     |
| F6  | Create Draft/Confirm + list                                                   | Confirm → schedule `FacilityRental`; Open → F7                         | Overlap tested in unit tests                                   |
| F7  | Status, agreement Preview/Attach, charges/payment, inspections + photo attach | Receipt → E2; PostRental gates Complete                                | Live Preview/print residual                                    |
| F8  | SfGrid recent inspections                                                     | Reservation button → F7                                                | Create path is F7 (by design)                                  |
| F9  | SfGrid + include-zero filter                                                  | Facility unit only                                                     | Refresh required after toggle (S3)                             |

**No S0/S1 found in code review** for F3–F9. Live clerk job stories still required before checking inventory boxes.

## Board summary

| Bucket                  |                                                Count |
| ----------------------- | ---------------------------------------------------: |
| PASS WITH NOTES (prior) |                            ~25 (A–E, F1–F2, G–I, H*) |
| Unchecked (CC Spec 002) |                                        **7** (F3–F9) |
| Out of scope            | DocuSign, portal/ACH, multi-property, public booking |

## Next surface (skill protocol)

**F3** `/community-center` — full session template on Mac Development (`./scripts/run-local.sh`), then F4.

Residual NAS: `docs/handover/NAS-CLERK-SMOKE.md`.
