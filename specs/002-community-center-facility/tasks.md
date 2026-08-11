# Tasks: Community Center Facility Operations

**Input**: Design documents from `/specs/002-community-center-facility/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/
**Tests**: Include unit tests for conflict detection, agreement PDF bytes, soft-delete rules, WO completer validation.

**Organization**: By user story for incremental delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1…US8 from spec.md

---

## Phase 1: Setup

- [x] T001 Confirm `SPECIFY_FEATURE=002-community-center-facility` and READINESS G1–G3 reviewed
- [x] T002 [P] Mark [READINESS.md](./READINESS.md) PASSED with date when owner agrees to start implement
- [x] T003 [P] Update [AGENTS.md](../../AGENTS.md) feature pointer to include 002 (keep 001 historical)

---

## Phase 2: Foundational (blocking)

**Checkpoint**: No US UI until domain + migration + DI registration compile.

- [x] T010 Create domain types: `FacilityRenter`, `FacilityReservation`, `FacilityInspection`, `FacilityInventoryItem` + enums under `src/Wiley.Apartments.Domain/`
- [x] T011 [P] Extend `ScheduledItemCategory` with `FacilityRental`; add `FacilityReservationId?` on `ScheduledItem`
- [x] T012 [P] Extend `MaintenanceRequest` with `CompletedByUserId?`, `CompletedByDisplay?`, `FacilityReservationId?`
- [x] T013 [P] Extend `LedgerEntry` with `FacilityReservationId?`, `FacilityRenterId?`; make `TenantId` nullable with exactly-one identity rule
- [x] T014 [P] Extend `DocumentEntityType` / `DocumentCategory` for facility entities
- [x] T015 EF configurations in `ApartmentsDbContext` + migration `AddCommunityCenterFacility`
- [x] T016 [P] Contracts: `IFacilityRenterService`, `IFacilityReservationService`, `IFacilityInspectionService`, `IFacilityInventoryService`, `IFacilityRentalAgreementService`
- [x] T017 Register services in `Program.cs`
- [x] T018 Update `DemoDataSeeder` Clear/Seed paths: CC uses FacilityRenter + FacilityReservation (remove CC Tenant/Lease creation); fix tests

**Checkpoint**: `dotnet build` + existing tests green (seeder tests updated).

---

## Phase 3: User Story 1 — Facility renter database (P1) 🎯 MVP slice A

- [x] T020 [US1] Implement `FacilityRenterService` (CRUD, search, soft-delete guard, audit)
- [x] T021 [P] [US1] Unit tests `FacilityRenterServiceTests`
- [x] T022 [US1] Syncfusion pages `/community-center/renters` + detail (MCP + skills before markup)
- [x] T023 [US1] Nav link **CC renters** in `MainLayout.razor`
- [x] T024 [US1] Ensure `/tenants` query excludes any legacy CC-tagged tenants documentation; FacilityRenters never listed there

---

## Phase 4: User Story 2 — Reservations + shared calendar (P1)

- [x] T030 [US2] Implement `FacilityReservationService` with overlap check + ScheduledItem upsert on Confirm
- [x] T031 [P] [US2] Unit tests for overlap / status transitions
- [x] T032 [US2] Reservation list + detail pages under `/community-center/reservations`
- [x] T033 [US2] Schedule service/UI: show FacilityRental category; CC filter includes reservations
- [x] T034 [US2] Hub quick link to Reservations

---

## Phase 5: User Story 3 — Rental agreement generate + store (P1)

- [x] T040 [US3] `FacilityRentalAgreementGenerator` (template and/or drawn PDF)
- [x] T041 [US3] `FacilityRentalAgreementService.GenerateAsync` + vault Document + audit
- [x] T042 [P] [US3] Unit test: non-empty PDF + required-field validation
- [x] T043 [US3] Reservation detail: Generate / Preview (SfPdfViewer) / Attach signed
- [x] T044 [US3] Add placeholder template path docs under `templates/cc-rental-agreement.pdf` (or generate-drawn-only until town supplies file)

---

## Phase 6: User Story 4 — Deposits, fees, receipts (P1)

- [x] T050 [US4] Ledger APIs: post facility deposit/fee charge + payment linked to reservation/renter
- [x] T051 [US4] Extend `PaymentReceiptService` / generator labels for CC rental payments
- [x] T052 [P] [US4] Tests: facility payment receipt generation
- [x] T053 [US4] Reservation detail money panel + CC payments filter UX copy
- [x] T054 [US4] Audit receipt generate for facility payments

**MVP Checkpoint (P1)**: Renter → Reserve → Agree → Pay → Receipt demonstrable on Mac.

---

## Phase 7: User Story 5 — Inspections (P2)

- [x] T060 [US5] `FacilityInspectionService` + validation
- [x] T061 [P] [US5] Tests for unsatisfactory ⇒ damage notes
- [x] T062 [US5] `/community-center/inspections` + reservation-linked create UI
- [x] T063 [US5] Optional photo upload via Document vault entity type FacilityInspection

---

## Phase 8: User Story 6 — Equipment inventory (P2)

- [x] T070 [US6] `FacilityInventoryService`
- [x] T071 [P] [US6] Tests CRUD + facility unit guard
- [x] T072 [US6] `/community-center/inventory` SfGrid (categories Kitchen/Chair/Table/Refrigerator/Oven/Fixture/Other)
- [x] T073 [US6] Seed starter inventory in demo seeder (chairs, tables, fridge, oven, kitchen basics)

---

## Phase 9: User Story 7 — Work order completion + history (P2)

- [x] T080 [US7] Update `MaintenanceService.CompleteAsync` to require/store CompletedBy*
- [x] T081 [US7] MaintenanceList complete dialog: completer field; history filter for CC
- [x] T082 [P] [US7] Tests for completer required
- [x] T083 [US7] Optional link WO ↔ FacilityReservation on create (CC)

---

## Phase 10: User Story 8 — Hub separation UX (P3)

- [x] T090 [US8] Expand `CommunityCenterHub.razor` cards/links for all CC tabs; remove “no separate reservation system” copy
- [x] T091 [US8] MainLayout Community Center section: Hub, Renters, Reservations, Schedule, Payments, Inspections, Inventory, Maintenance, Documents
- [x] T092 [US8] Update `docs/clerk-quick-reference.md` CC section for new workflows
- [x] T093 [US8] Update `scripts/clerk-acceptance-smoke.md` with CC P1 path
- [x] T094 [US8] Mark 001 tasks.md NV-3 as moved to 002 (done-by-reference)

---

## Phase 11: Polish & acceptance

- [x] T100 Function inventory surfaces update for new CC pages (skill: function-inventory)
- [x] T101 Full `dotnet test`
- [x] T102 Manual Mac smoke per quickstart; note evidence date in READINESS or handover
- [x] T103 Suggest `/code-review` incremental pass after P1 MVP and again after P2

---

## Dependencies

```text
Phase1 → Phase2 → US1 → US2 → US3 → US4  (P1 MVP)
                ↘ US5 → US6 → US7 → US8 → Polish
```

US5–US7 may proceed in parallel after T018 if staffing allows, but agreement/money should land before calling facility “rental ready.”

## Parallel examples

- T011–T014 after T010 types exist
- T021 // T022 after T020
- T031 // T032 after T030
- T061 // T062 after T060

---

## Phase 12: Convergence

- [x] T104 Add reservation agreement Preview (`ClerkPdfViewer` / SfPdfViewer) and Attach signed upload UI on `FacilityReservationDetail` wiring `IFacilityRentalAgreementService.AttachSignedAsync` without clearing generated path per FR-005 / US3/AC1–AC2 (`partial`)
- [x] T105 Show Completer (+ cost) on maintenance history grid and enable date-range filter for CC history views per FR-009 / US7/AC3 (`partial`)
- [x] T106 Add unit test covering facility payment receipt generation / CC rental labels per T052 / US4 Independent Test (`partial`)
- [x] T107 Add unit test that `MaintenanceService.CompleteAsync` rejects missing `CompletedByDisplay` per T082 / edge case Completer required (`partial`)
- [x] T108 Add inspection document/photo attach path from reservation or inspections UI (FacilityInspection vault entity) per FR-007 / T063 (`partial`)
- [x] T109 Update `clerk-suite-surfaces.md` inventory with CC renters, reservations, inspections, inventory routes per T100 (`partial`)
- [x] T110 Record Mac Development smoke evidence date for post-rental inspection + WO completer in READINESS or handover per T102 / SC-005 / plan proven path (`partial`)
- [x] T111 Expose optional FacilityReservation link on maintenance create (CC) per T083 / US7 (`partial`)
- [x] T112 Add inventory “include zero quantity” filter per edge case Inventory quantity zero (`partial`)
- [x] T113 Document CC agreement preview / signed-upload steps in `docs/clerk-quick-reference.md` per plan proven agreement path (`partial`)
