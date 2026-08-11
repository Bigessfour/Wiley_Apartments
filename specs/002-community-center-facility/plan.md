# Implementation Plan: Community Center Facility Operations

**Branch**: `002-community-center-facility` | **Date**: 2026-08-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-community-center-facility/spec.md`
**Absorbs**: 001 **NV-3** (Facility reservation + CC rental agreement PDF)

## Summary

Separate Community Center operations from residential apartments with a dedicated `FacilityRenter` + `FacilityReservation` model, CC-only tabs, shared calendar for rentals/maintenance, agreement PDF + vault storage, deposit/fee receipts, inspections, facility inventory, and work-order completion attribution. Reuse ClerkSuite 001 stack (Blazor Interactive Server, Syncfusion, EF Core SQLite, NAS docs, AuditLog).

## Technical Context

**Language/Version**: .NET 9 / C#
**Primary Dependencies**: ASP.NET Core Blazor Interactive Server, Syncfusion Blazor, Syncfusion PDF, EF Core, ASP.NET Identity
**Storage**: SQLite (default) on Docker volume; documents on NAS `/volume1/apartments/docs`
**Testing**: xUnit + FluentAssertions (`tests/Wiley.Apartments.Tests`, IntegrationTests)
**Target Platform**: Mac Development day-to-day; Synology DS225+ `linux/amd64` for milestone deploy
**Project Type**: Existing single web app `src/Wiley.Apartments.Web` + Domain/Contracts
**Performance Goals**: 2 concurrent clerks; app RSS ≤ ~1.5 GiB on NAS
**Constraints**: Syncfusion-only UI; no public booking portal; no e-sign; Keychain secrets never in Spec Kit
**Scale/Scope**: 1 facility unit (CC), modest renter/reservation volume (dozens/year), ~16 residential units unchanged

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle            | Status                                                                        |
| -------------------- | ----------------------------------------------------------------------------- |
| I Clerk-first        | Pass — CC tabs for all facility workflows                                     |
| II NAS residency     | Pass — SQLite + docs paths under community-center/                            |
| III Auditability     | Pass — FR-010 AuditLog on mutations                                           |
| IV Minimal parts     | Pass — extend Ledger/Maintenance/Schedule; no new services host               |
| V Syncfusion         | Pass — SfGrid/SfSchedule/SfPdfViewer/SfButton/Cards; MCP + skills on UI tasks |
| VI Security          | Pass — authenticated clerks; no new public surface                            |
| VII Colorado leasing | N/A for hall hire — town attorney reviews CC template separately              |
| VIII Demonstrable    | Pass — quickstart + acceptance tasks                                          |

No constitution violations requiring Complexity Tracking entries.

## Project Structure

### Documentation (this feature)

```text
specs/002-community-center-facility/
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── READINESS.md         # Implement gate
├── contracts/
│   └── services.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2
```

### Source Code (repository — additive)

```text
src/Wiley.Apartments.Domain/
  FacilityRenter.cs
  FacilityReservation.cs
  FacilityInspection.cs
  FacilityInventoryItem.cs
  FacilityReservationStatus.cs
  FacilityInspectionType.cs
  FacilityInventoryCategory.cs
  (+ enum/extensions on ScheduledItemCategory, DocumentEntityType, …)

src/Wiley.Apartments.Contracts/
  IFacilityRenterService.cs
  IFacilityReservationService.cs
  IFacilityInspectionService.cs
  IFacilityInventoryService.cs
  (+ receipt/agreement DTOs as needed)

src/Wiley.Apartments.Web/
  Data/Migrations/*Facility*
  Services/FacilityRenterService.cs
  Services/FacilityReservationService.cs
  Services/FacilityInspectionService.cs
  Services/FacilityInventoryService.cs
  Services/FacilityRentalAgreementGenerator.cs
  Services/FacilityRentalAgreementService.cs
  Components/Pages/CommunityCenter/
    CommunityCenterHub.razor          # expand
    Renters/*.razor
    Reservations/*.razor
    Inspections/*.razor
    Inventory/*.razor
  Components/Layout/MainLayout.razor  # CC nav tabs
  Services/DemoDataSeeder.cs          # FacilityRenter path
  Services/MaintenanceService.cs      # CompletedBy
  Services/ScheduleService.cs         # FacilityRental + FK
  Services/PaymentReceipt*            # facility labels

tests/Wiley.Apartments.Tests/Services/Facility*.cs
```

## Implementation approach

1. **Foundation**: Domain + EF migration + service interfaces; Ledger/Maintenance/Schedule column adds; Document enum extends.
2. **P1 vertical**: Renters → Reservations (+ calendar conflict) → Agreement PDF → Ledger charges/payments/receipts.
3. **P2 vertical**: Inspections → Inventory → WO CompletedBy + CC history UX.
4. **P3**: Hub/nav polish, demo seeder rewrite, clerk quick-reference, acceptance smoke.
5. **UI rule**: Every new `.razor` page uses Syncfusion MCP (`sf_blazor_assistant`) + component skills before merge.

## Proven agreement path (acceptance of “needs to be proven”)

1. Unit test: generator produces non-empty PDF bytes for sample merge data.
2. Integration: service writes file under DocumentRoot and creates Document row.
3. Manual: clerk preview in SfPdfViewer Print on Mac Development (`./scripts/run-local.sh`).
4. Evidence note in `docs/clerk-quick-reference.md` CC section.

## Complexity Tracking

> No constitution violations requiring justification.
