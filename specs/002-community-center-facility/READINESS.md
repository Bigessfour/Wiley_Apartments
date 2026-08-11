# READINESS: 002-community-center-facility

**Gate status**: **PASSED** — 2026-08-11 (G1–G3 confirmed; `/speckit-implement` started).

## G1 — Spec Kit artifacts

| Artifact              | Present |
| --------------------- | ------- |
| spec.md               | Yes     |
| plan.md               | Yes     |
| research.md           | Yes     |
| data-model.md         | Yes     |
| contracts/services.md | Yes     |
| tasks.md              | Yes     |
| quickstart.md         | Yes     |

## G2 — Constitution

Same as 001: Syncfusion-only, NAS data, audit, no keys in repo. Confirm T0.0 toolchain still valid (MCP + license) before `.razor` work.

## G3 — Decisions locked

| Topic          | Decision                                    |
| -------------- | ------------------------------------------- |
| Renter model   | New `FacilityRenter`                        |
| Booking model  | New `FacilityReservation` (not Lease)       |
| Calendar       | Shared SfSchedule + FacilityRental category |
| E-sign         | Out of scope                                |
| Public booking | Out of scope                                |
| Inventory      | `FacilityInventoryItem`                     |
| WO completer   | `CompletedByDisplay` (+ optional user id)   |

## G4 — Implement start

Start at **tasks.md Phase 1–2 (foundation)**, then US1. Do not mix CC Tenant/Lease demo data after seeder rewrite.

**Passed when**: Owner/agent checks this file to **PASSED** with date after confirming G1–G3.

## Smoke evidence (T110 / SC-005)

| Check                                      | Evidence                                                                                                                                                                                                                    |
| ------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Mac Development smoke (quickstart CC path) | **2026-08-11** — unit tests green for overlap, facility receipt vault path, Completer required; UI paths: reservation Preview/Attach signed, inspection attach, maintenance Completer + date filter, inventory include-zero |
| Post-rental + WO completer                 | Demonstrable on Mac via `/community-center/reservations/{id}` + `/maintenance?unitId=` (CC); NAS dual-clerk still per `docs/handover/NAS-CLERK-SMOKE.md`                                                                    |
