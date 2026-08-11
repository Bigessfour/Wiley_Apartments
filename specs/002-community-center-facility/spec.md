# Feature Specification: Community Center Facility Operations

**Feature Branch**: `002-community-center-facility`

**Created**: 2026-08-11

**Status**: Draft — ready for plan / tasks / implement

**Input**: Separate Community Center (CC) operations from residential apartments while sharing one operations calendar. Track facility renters, rental dates, rental agreements, deposits/fees with receipts, pre/post-rental inspections, equipment inventory, maintenance history, and work-order completion. Absorbs deferred **NV-3** from `001-wiley-apartment-v1`.

**Baseline**: ClerkSuite 001 is done for residential (units, tenants, leases, ledger, schedule, maintenance, documents). CC today is a thin hub that filters shared pages onto facility unit `Number=CC` (`IsFacility`). Demo data incorrectly models CC renters as `Tenant` + `Lease`.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Facility renter database (Priority: P1)

Clerks maintain a Community Center **renter** directory (not residential tenants). Each renter stores identity and contact data used on agreements and receipts: legal name, organization (optional), mailing address, phone, email, emergency/alternate contact, ID type/number (optional), notes, and soft-delete.

**Why this priority**: Every rental, agreement, receipt, and calendar booking hangs off the renter record. Without a clean entity, CC data continues to pollute residential Tenants.

**Independent Test**: Create/search/edit a FacilityRenter from CC tabs only; confirm the person does **not** appear on residential Tenants list.

**Acceptance Scenarios**:

1. **Given** an authenticated clerk on Community Center → Renters, **When** they create a renter with name, address, phone, and email, **Then** the record saves, appears in CC renter search, and is absent from `/tenants`.
2. **Given** an existing renter, **When** the clerk soft-deletes them, **Then** they are hidden from default search but retained for history/audit.
3. **Given** demo seed after this feature, **When** CC demo renters load, **Then** they are `FacilityRenter` rows (not `Tenant`).

---

### User Story 2 - Facility reservations on shared calendar (Priority: P1)

Clerks book the Community Center for event rentals with start/end, renter, fee, deposit, status (Request/Confirmed/Cancelled/Completed), and notes. Confirmed rentals appear on the **shared** operations calendar (same `SfSchedule` as apartments) filtered/tagged as facility rentals. Maintenance and residential schedule items remain visible on the shared calendar; CC nav tabs show CC-scoped views.

**Why this priority**: Avoiding double-booking and making rental dates visible is the core operational need.

**Independent Test**: Create a confirmed reservation; open Schedule (all) and CC schedule; see the rental; create overlapping booking and get a conflict rejection.

**Acceptance Scenarios**:

1. **Given** a FacilityRenter and vacant CC window, **When** clerk creates a Confirmed reservation, **Then** a calendar item appears for those dates linked to the reservation and CC unit.
2. **Given** an existing Confirmed reservation, **When** clerk tries an overlapping Confirmed booking on CC, **Then** the system blocks with a clear conflict message.
3. **Given** CC hub, **When** clerk opens CC schedule, **Then** they see CC unit events (rentals + CC maintenance/prep) without needing the residential tenant list.

---

### User Story 3 - Rental agreement generate + store (Priority: P1)

Clerks generate a Community Center rental agreement PDF from a NAS template (or Syncfusion-drawn fallback), preview/print it, and store the generated file in the document vault linked to the reservation. Signed copy can be uploaded later (e-sign remains out of scope — same posture as residential leases).

**Why this priority**: Town needs a proven, repeatable agreement artifact before collecting deposits.

**Independent Test**: Generate agreement for a draft/confirmed reservation; PDF opens in SfPdfViewer; vault document exists; regenerate stays audited.

**Acceptance Scenarios**:

1. **Given** a reservation with renter + dates + fee/deposit, **When** clerk clicks Generate agreement, **Then** PDF is produced, path stored on the reservation, and vault metadata links to the reservation.
2. **Given** a generated agreement, **When** clerk uploads a signed PDF, **Then** it attaches as the signed document for that reservation without deleting the generated draft.
3. **Given** missing required renter fields, **When** generate is attempted, **Then** the UI lists missing fields and does not write a partial PDF.

---

### User Story 4 - Deposit / fee charges and receipts (Priority: P1)

Clerks record rental fee and damage-deposit charges and payments against a facility reservation (and renter). They generate printable payment receipts for deposit and fee payments using the existing receipt pattern (town header, amount, method, date).

**Why this priority**: Money handling must be auditable and clerk-demonstrable.

**Independent Test**: Post deposit charge + payment; generate receipt; confirm receipt labels CC rental (not residential rent).

**Acceptance Scenarios**:

1. **Given** a Confirmed reservation, **When** clerk posts deposit and fee charges, **Then** ledger entries are tied to the reservation + FacilityRenter + CC unit.
2. **Given** a payment against those charges, **When** clerk opens Receipt, **Then** PDF receipt generates and can print/save to vault.
3. **Given** CC payments tab, **When** clerk filters by CC, **Then** only facility-linked ledger activity shows (not residential rent rolls mixed as primary UX).

---

### User Story 5 - Pre / post rental inspection (Priority: P2)

Before re-rental (and optionally after an event), clerks record an inspection: reservation link, type (PreRental / PostRental), overall satisfactory yes/no, checklist notes, damage notes, inspector (clerk user), timestamp, and optional photos via documents.

**Why this priority**: Deposit refund / damage decisions depend on documented condition.

**Independent Test**: Complete post-rental inspection “satisfactory”; reservation can move to Completed; unsatisfactory inspection requires damage notes before deposit refund workflow.

**Acceptance Scenarios**:

1. **Given** a past Confirmed reservation, **When** clerk records PostRental inspection as satisfactory, **Then** the inspection is stored and visible on the reservation detail.
2. **Given** unsatisfactory condition, **When** clerk saves inspection without damage notes, **Then** validation fails.
3. **Given** CC Inspections tab, **When** clerk lists recent inspections, **Then** they see date, renter, satisfactory flag, and link to reservation.

---

### User Story 6 - Facility equipment inventory (Priority: P2)

Clerks inventory CC equipment on Community Center tabs: kitchen items, chairs, tables, refrigerators, ovens, fixtures, and other. Each item has type/category, description, quantity (for countables), condition, location (e.g. Kitchen, Hall, Storage), serial (optional), and notes. History of condition changes is auditable.

**Why this priority**: Setup and damage claims need a known inventory baseline.

**Independent Test**: Add 50 chairs + kitchen mixer on CC Inventory; list/filter by category; edit quantity/condition.

**Acceptance Scenarios**:

1. **Given** CC Inventory tab, **When** clerk adds “Folding chairs” quantity 50 condition Good, **Then** the item appears under category Chairs.
2. **Given** existing inventory, **When** clerk changes condition to Damaged, **Then** AuditLog records before/after.
3. **Given** residential unit detail, **When** viewing assets, **Then** CC facility inventory is not mixed into apartment appliance lists as the primary path (CC inventory lives under CC tabs).

---

### User Story 7 - Maintenance history and work-order completion (Priority: P2)

Clerks create work orders against the CC facility (optionally linked to an inventory item or reservation). They track status Open → InProgress → Completed/Cancelled, cost, notes, **who completed** the work, and completion time. Maintenance history is listable from CC Maintenance. Shared calendar can show maintenance windows on CC.

**Why this priority**: Facility readiness depends on tracked repairs and clear completion attribution.

**Independent Test**: Open WO on CC, complete with completer name/user; history shows completer; dashboard open-WO list includes CC items when open.

**Acceptance Scenarios**:

1. **Given** CC Maintenance, **When** clerk creates a work order “Replace kitchen faucet”, **Then** it is stored on CC unit with Open status.
2. **Given** an InProgress work order, **When** clerk Completes and enters completer, **Then** CompletedUtc and CompletedBy are set and AuditLog records the transition.
3. **Given** completed WOs, **When** clerk opens CC maintenance history, **Then** they can filter by date range and see completer + cost.

---

### User Story 8 - CC hub separation UX (Priority: P3)

Community Center sidebar exposes dedicated tabs: Hub, Renters, Reservations, Schedule, Payments, Inspections, Inventory, Maintenance, Documents (CC-filtered where metadata allows). Apartment workflows remain under Units/Tenants/Leases. Shared calendar remains one Schedule page with unit/category filters — not a second Syncfusion schedule product.

**Why this priority**: Separation is a product principle; deep links reduce clerk confusion.

**Independent Test**: Navigate each CC tab; confirm no residential tenant CRUD on those routes.

**Acceptance Scenarios**:

1. **Given** sidebar Community Center section, **When** clerk opens each CC tab, **Then** pages load scoped to facility data.
2. **Given** residential Tenants, **When** listing, **Then** FacilityRenters never appear.
3. **Given** Hub, **When** facility unit missing, **Then** clear seed/restart guidance (same as today).

---

### Edge Cases

- Overlapping Confirmed reservations on CC → reject; Draft/Request may warn but not block until Confirmed (recommended).
- Cancelling a reservation with paid deposit → charges remain; clerk handles refund ledger explicitly (no silent delete).
- Soft-deleted FacilityRenter with future reservation → block delete or force cancel reservations first.
- Agreement generate without NAS template → drawn PDF fallback (prove path); template preferred when present under `templates/cc-rental-agreement.*`.
- Work order complete without completer → require CompletedByDisplay (typed name) even if Identity user id unavailable.
- Inventory quantity zero → allowed (retired/out); still listed when “include zero” filter on.
- Concurrent edit on reservation → RowVersion conflict message (match 001 pattern).

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a `FacilityRenter` entity and CC-only CRUD UI separate from residential `Tenant`.
- **FR-002**: FacilityRenter MUST store at minimum: first/last name, organization (optional), mailing address, phone, email, alternate/emergency contact, optional ID type + last-4 or reference, notes, soft-delete, RowVersion.
- **FR-003**: System MUST provide `FacilityReservation` with renter, CC unit, start/end UTC, status, rental fee, deposit amount, notes, agreement paths, RowVersion.
- **FR-004**: Confirmed reservations MUST appear on the shared operations calendar linked to the CC unit; conflict detection MUST prevent overlapping Confirmed bookings on CC.
- **FR-005**: System MUST generate and store CC rental agreement PDFs; vault documents MUST link to the reservation; signed upload MUST be supported without e-sign provider.
- **FR-006**: System MUST post and display deposit and rental-fee ledger activity for facility reservations; payment receipts MUST be generatable for those payments.
- **FR-007**: System MUST support FacilityInspection (PreRental / PostRental) with satisfactory flag, notes, inspector, timestamps, and document attachments.
- **FR-008**: System MUST support CC equipment inventory (categories including kitchen items, chairs, tables, refrigerators, ovens, fixtures, other) with quantity and condition, managed from CC Inventory tab.
- **FR-009**: System MUST support work-order create/status/complete against CC with **CompletedBy** attribution and listable maintenance history.
- **FR-010**: All CC create/update/delete of renters, reservations, inspections, inventory, agreements, receipts, and maintenance MUST write AuditLog entries.
- **FR-011**: UI MUST keep CC data under Community Center tabs; residential pages MUST not present FacilityRenters as Tenants.
- **FR-012**: Demo seeder MUST create FacilityRenter + FacilityReservation demo data (migrate off Tenant/Lease for CC).
- **FR-013**: Shared calendar remains one Schedule surface; CC schedule is a filtered view — no second standalone calendar product.

### Key Entities

- **FacilityRenter**: Event/hall hirer identity (not a residential tenant).
- **FacilityReservation**: Booking of CC for a date range with money and agreement linkage.
- **FacilityInspection**: Condition check tied to a reservation (and optionally inventory notes).
- **FacilityInventoryItem**: Countable/serialized equipment belonging to CC (extends or replaces misuse of residential Asset UX for CC).
- **MaintenanceRequest** (enhanced): Existing WO entity + CompletedBy fields; CC history filtered by facility unit.
- **ScheduledItem** (enhanced): Category for FacilityRental; link to FacilityReservationId.
- **LedgerEntry** (enhanced): Optional FacilityReservationId / FacilityRenterId for CC money (residential TenantId nullable when facility-only).
- **Document**: Categories/entity types for FacilityReservation, FacilityRenter, FacilityInspection, FacilityInventoryItem.

### Non-goals (this feature)

- Public self-service online booking portal for citizens.
- DocuSign / e-sign (remains NV-2 / future).
- Membership pricing engines, alcohol-permit workflows, multi-room resource hierarchy.
- Separate second database or second Blazor app for CC.
- Dedicated CC P&L report page (may reuse ops costs later).

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Clerk creates a renter + confirmed reservation + agreement PDF in under 5 minutes on a training run.
- **SC-002**: Overlapping Confirmed CC bookings are rejected 100% of the time in automated tests.
- **SC-003**: 100% of CC demo renters after reseeding are FacilityRenter (zero CC-tagged Tenants required for demo).
- **SC-004**: Payment receipt for a CC deposit opens in SfPdfViewer and prints without watermark/license errors (Syncfusion licensed).
- **SC-005**: Post-rental inspection + WO complete-with-completer are demonstrable on Mac Development and documented for NAS acceptance.
- **SC-006**: Residential Tenants list shows no FacilityRenters after feature ship.

## Assumptions

- Single CC facility unit (`Number=CC`, `IsFacility=true`) remains the bookable space for v1 of this feature.
- Clerks (not public) create all reservations — request/approval is internal status, not a public portal.
- Pricing is clerk-entered per reservation (no automated member/nonprofit rate engine).
- Deposit refund is a normal ledger payment/adjustment performed by clerk after satisfactory inspection — not an automated Stripe flow.
- NAS document root and Syncfusion stack from 001 remain in force.
- Constitution principles I–VIII apply unchanged; Syncfusion-only UI; audit append-only; NAS residency.
