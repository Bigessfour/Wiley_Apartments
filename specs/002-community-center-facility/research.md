# Research: Community Center Facility Operations (002)

**Date**: 2026-08-11
**Feature**: `002-community-center-facility`
**Sources**: Current ClerkSuite codebase (001), Communal facility reservation guide, Skedda facility reservation buyer’s guide, Locality UK “Managing your community building” (hire agreements / inventory).

---

## Decision 1: New FacilityRenter entity (not Tenant flag)

**Decision**: Introduce `FacilityRenter` as a first-class domain type. Stop modeling CC hirers as `Tenant` + short `Lease`.

**Rationale**: User requirement for separation; residential Tenant carries household/vehicles/pets/security-deposit semantics that do not apply to hall rentals. Demo seeder already pollutes Tenants with CC notes tags.

**Alternatives considered**: `Tenant.IsFacilityRenter` flag — rejected (mixes lists, lease semantics, deposit panels). Dual-write both — rejected (complexity).

---

## Decision 2: FacilityReservation instead of Lease for CC

**Decision**: New `FacilityReservation` entity for bookings. Do not reuse `Lease` for hall hire.

**Rationale**: Leases imply Colorado residential terms, renew/amend/terminate lifecycle, and monthly rent. Hall hire is short-dated, fee+deposit, agreement template distinct from Brookside residential templates.

**Calendar bridge**: On confirm, upsert `ScheduledItem` with category `FacilityRental`, `UnitId=CC`, `FacilityReservationId`, optional display title including renter name.

---

## Decision 3: One shared calendar (filter, don’t fork)

**Decision**: Keep single `/schedule` `SfSchedule`. Add FacilityRental category + CC deep links. No second schedule control.

**Rationale**: User asked to share calendar for maintenance and rentals; 001 already deferred “separate SfSchedule”. Industry guides treat calendar as one availability truth with blackouts/maintenance.

---

## Decision 4: Agreement PDF — prove with Syncfusion path (clerk sign offline)

**Decision**: Mirror residential lease generate: template under NAS `templates/` when present; Syncfusion PDF draw/fill fallback; vault store; signed upload. No DocuSign in 002.

**Rationale**: Spec says “proven”; residential `LeaseService` + `PaymentReceiptGenerator` are known-good patterns. E-sign remains NV-2.

**Best practice (Communal / Locality)**: Agreement attached to the booking; T&Cs cover deposits, cancellation, insurance/liability, use restrictions, cleanup. Template fields: parties, dates/times, fee, deposit, premises rules, signature lines.

---

## Decision 5: Money — LedgerEntry links to reservation + FacilityRenter

**Decision**: Extend `LedgerEntry` with optional `FacilityReservationId` and `FacilityRenterId`. Reuse receipt generator with facility-aware labels. Keep `IsDeposit` for damage deposits.

**Rationale**: Avoid a second accounting subsystem (constitution: minimal moving parts). CC payments tab filters by CC unit / facility links.

---

## Decision 6: Inspections as FacilityInspection entity

**Decision**: New `FacilityInspection` linked to `FacilityReservation` (required), with PreRental/PostRental, IsSatisfactory, DamageNotes, InspectorUserId/Display, timestamps. Photos via Document vault.

**Rationale**: Industry practice ties post-event inspection to deposit release. Scheduling category `Inspection` alone is not enough structured data.

---

## Decision 7: Inventory — FacilityInventoryItem on CC (not only Asset grid)

**Decision**: New `FacilityInventoryItem` for CC equipment with Category enum (Kitchen, Chair, Table, Refrigerator, Oven, Fixture, Other), Quantity, Condition, Location. Optionally allow `AssetId` link later; do not force residential Asset UX as the only CC path.

**Rationale**: Chairs/tables need quantity; residential Asset is appliance-serial oriented. Locality guidance: inventory appliances + manuals + safety. CC Inventory tab is the clerk surface.

**Migration note**: Existing CC `Asset` rows (if any) can be left or one-time copied in a seeder task; not blocking.

---

## Decision 8: Work orders — enhance MaintenanceRequest

**Decision**: Add `CompletedByUserId` (nullable) + `CompletedByDisplay` (required on complete) + optional `FacilityReservationId`. Keep Open/InProgress/Completed/Cancelled. CC history = filter `Unit.IsFacility`.

**Rationale**: Completion attribution was missing; user explicitly asked who completed. Creating a parallel WO table would duplicate Phase 5 work.

**Best practice (Skedda)**: Approved events trigger setup/cleaning tasks — optional later: auto-create ScheduledItem prep on confirm (nice-to-have task, not P1).

---

## Decision 9: Scope fence (NAS / clerks)

**Decision**: Clerk-only booking (no public portal). Single hall resource. Clerk-entered pricing. No membership rate engine. Stay on SQLite + Syncfusion Interactive Server.

**Rationale**: Constitution IV (minimal parts), DS225+ RAM fence, two concurrent clerks.

---

## Industry checklist → ClerkSuite mapping

| Best-practice capability                     | In 002? | How                                           |
| -------------------------------------------- | ------- | --------------------------------------------- |
| Real-time availability / conflict prevention | Yes     | Reservation overlap check + shared calendar   |
| Renter database                              | Yes     | FacilityRenter                                |
| Agreements attached to booking               | Yes     | Generate + vault on FacilityReservation       |
| Deposits + fees + receipts                   | Yes     | Ledger + PaymentReceipt (facility-aware)      |
| Post-rental inspection / damage              | Yes     | FacilityInspection                            |
| Equipment inventory                          | Yes     | FacilityInventoryItem                         |
| Work orders / completion                     | Yes     | MaintenanceRequest + CompletedBy              |
| Setup/cleaning tasks from booking            | Partial | Manual ScheduledItem; optional auto-prep task |
| Public self-serve booking                    | No      | Out of scope                                  |
| E-sign / COI automation                      | No      | NV-2 / future                                 |
| Multi-space hierarchy                        | No      | Single CC unit                                |

---

## Current-state gap inventory (001 → 002)

| Capability             | 001 today                         | Gap                                                        |
| ---------------------- | --------------------------------- | ---------------------------------------------------------- |
| CC hub + nav filters   | Yes — thin deep links             | Expand tabs for renters/reservations/inspections/inventory |
| Facility unit CC       | Yes `IsFacility`                  | Keep                                                       |
| Shared schedule        | Yes `ScheduledItem`               | Add FacilityRental category + reservation FK               |
| CC “renters”           | Demo as Tenant+Lease              | Replace with FacilityRenter + FacilityReservation          |
| Rental agreement PDF   | Deferred NV-3                     | Build CC generator + vault                                 |
| Deposit/fee receipts   | Payment receipts exist for ledger | Wire facility payments + labels                            |
| Inspection entity      | Schedule category only            | FacilityInspection                                         |
| Equipment inventory UX | Unit Asset grid                   | FacilityInventoryItem + CC Inventory page                  |
| WO completion who      | Status + cost only                | CompletedBy fields + UI                                    |
| Data separation        | Filters only                      | Enforce entity + UI separation                             |

---

## Risks

| Risk                           | Mitigation                                                                           |
| ------------------------------ | ------------------------------------------------------------------------------------ |
| Demo/tests assume CC Tenants   | Update DemoDataSeeder + tests in foundational phase                                  |
| Ledger TenantId required today | Make TenantId nullable when FacilityRenterId set; migrate carefully                  |
| Template legal review          | Ship fillable/drawn PDF marked for town attorney review; clerks edit template on NAS |
| Scope creep (public portal)    | Non-goals in spec; reject in tasks                                                   |
