# Data Model: ClerkSuite v1

**Date**: 2026-08-09 | **Feature**: 001-wiley-apartment-v1 | **Units**: 16 (fixed portfolio)

**Timezone**: Store UTC; display `America/Denver`

## Core Entities (canonical)

| Entity                 | C# type (planned)    | Key fields                                                                                                                      |
| ---------------------- | -------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| **Unit**               | `Unit`               | Id, Number, SqFt, Beds, Baths, Status, Notes, CurrentTenantId, MonthlyRent, SecurityDeposit, IsHandicapAccessible, LeaseTerm, IsFacility, RowVersion |
| **Asset / Appliance**  | `Asset`              | UnitId, Type, Make, Model, Serial, InstallDate, WarrantyStart, WarrantyEnd, Condition, PhotoPaths                               |
| **Flooring**           | `Flooring`           | UnitId, Type, InstallDate, Condition, ReplacedDate, Notes                                                                       |
| **Tenant**             | `Tenant`             | Id, Name fields, Phone, Email, EmergencyContact, MailingAddress, Notes, IsDeleted, RowVersion                               |
| **Occupancy**          | `Occupancy`          | TenantId, UnitId, StartDate, EndDate                                                                                            |
| **Lease**              | `Lease`              | Id, UnitId, TenantId, Start, End, Rent, Deposit, Status, TemplateUsed, IsDeleted, RowVersion                                    |
| **Charge**             | `LedgerEntry`        | LeaseId?, TenantId, UnitId, Amount, Type=Charge, Date, Notes                                                                    |
| **Payment**            | `LedgerEntry`        | LeaseId?, TenantId, UnitId, Amount, Type=Payment, Date, Method, Notes                                                           |
| **UnitOperatingCost**  | `UnitOperatingCost`  | UnitId?, Category (Utility/Repair/Replace/CommonUpkeep), Amount, IncurredUtc, Vendor?, Notes?, MaintenanceRequestId?, IsDeleted |
| **ScheduledItem**      | `ScheduledItem`      | Title, Category, UnitId?, TenantId?, LeaseId?, StartUtc, EndUtc?, DueUtc?, ReminderOffset, IsCompleted, IsDeleted               |
| **MaintenanceRequest** | `MaintenanceRequest` | UnitId, AssetId?, Description, Status, Cost, CompletedDate, Priority                                                            |
| **Document**           | `Document`           | EntityType, EntityId, FilePathOnNas, Category, UploadedBy, UploadedAt                                                           |
| **AuditLog**           | `AuditLog`           | User, Timestamp, Entity, Action, OldValues, NewValues                                                                           |
| **User**               | `ApplicationUser`    | Identity user for clerks (Clerk / ReadOnly / Elevated roles)                                                                    |

Supporting types: `HouseholdMember`, `Vehicle`, `Pet`, `LateFeeRule` (from spec FR-2 / FR-4).

**UnitOperatingCost** is landlord expense tracking (T4.5). It MUST NOT share storage with tenant `LedgerEntry` balances. `UnitId` may be null only when Category is `CommonUpkeep` (building-wide).

**Portfolio P/L (computed, T6.4):** No separate entity. Aggregate **income** from tenant `LedgerEntry` payments (and rent charges as billed income per report rule) minus **expense** from `UnitOperatingCost` (+ optional maintenance costs when Phase 5 links). CommonUpkeep without `UnitId` allocated evenly across 16 units for per-apt charts unless a unit is specified. Monthly/yearly series for city council packets.

---

## Entity Relationship Overview

```mermaid
erDiagram
    Unit ||--o{ Asset : contains
    Unit ||--o{ Flooring : has
    Unit ||--o{ Occupancy : tracks
    Unit ||--o{ MaintenanceRequest : has
    Tenant ||--o{ Occupancy : occupies
    Tenant ||--o{ Lease : signs
    Tenant ||--o{ LedgerEntry : ledger
    Unit ||--o{ Lease : subject
    Unit ||--o{ LedgerEntry : attributed
    Lease ||--o{ LedgerEntry : charges
    Asset ||--o{ MaintenanceRequest : serviced
    Document }o--|| Unit : "EntityType polymorphic"
    Document }o--|| Tenant : "EntityType polymorphic"
    AuditLog }o--|| User : by

    Unit {
        guid Id PK
        string Number UK
        decimal SqFt
        int Beds
        int Baths
        string Status
        string Notes
        guid CurrentTenantId FK
        bool IsFacility
        rowversion RowVersion
    }

    Asset {
        guid Id PK
        guid UnitId FK
        string Type
        string Make
        string Model
        string Serial
        date InstallDate
        date WarrantyStart
        date WarrantyEnd
        string Condition
    }

    Flooring {
        guid Id PK
        guid UnitId FK
        string Type
        date InstallDate
        string Condition
        date ReplacedDate
        string Notes
    }

    Tenant {
        guid Id PK
        string FirstName
        string LastName
        string Phone
        string Email
        string EmergencyContact
        string Notes
        bool IsDeleted
        rowversion RowVersion
    }

    Occupancy {
        guid Id PK
        guid UnitId FK
        guid TenantId FK
        datetime StartDate UTC
        datetime EndDate UTC
    }

    Lease {
        guid Id PK
        guid UnitId FK
        guid TenantId FK
        datetime Start UTC
        datetime End UTC
        decimal Rent
        decimal Deposit
        string Status
        string TemplateUsed
        bool IsDeleted
        rowversion RowVersion
    }

    LedgerEntry {
        guid Id PK
        string EntryType
        guid LeaseId FK
        guid TenantId FK
        guid UnitId FK
        decimal Amount
        datetime Date UTC
        string Method
        string Notes
    }

    MaintenanceRequest {
        guid Id PK
        guid UnitId FK
        guid AssetId FK
        string Description
        string Status
        decimal Cost
        datetime CompletedDate UTC
    }

    Document {
        guid Id PK
        string EntityType
        guid EntityId
        string FilePathOnNas
        string Category
        string UploadedBy
        datetime UploadedAt UTC
    }

    AuditLog {
        long Id PK
        string UserId
        datetime Timestamp UTC
        string EntityType
        string EntityId
        string Action
        json OldValues
        json NewValues
    }
```

---

## Enumerations

### UnitStatus

`Occupied` · `Vacant` · `Maintenance` · `MakeReady`

### LeaseStatus

`Draft` · `Active` · `Amended` · `Renewed` · `Terminated` · `Expired`

### LedgerEntryType

`Charge` · `Payment`

### PaymentMethod

`Cash` · `Check` · `Online` · `Other`

### DocumentEntityType

`Unit` · `Tenant` · `Asset` · `Lease` · `MaintenanceRequest`

### DocumentCategory

`Lease` · `Notice` · `Warranty` · `Manual` · `Receipt` · `InspectionPhoto` · `Screening` · `Correspondence` · `Other`

---

## Business Rules

| Rule               | Enforcement                                                              |
| ------------------ | ------------------------------------------------------------------------ |
| Max 16 units       | Seed 16; block create beyond cap                                         |
| UTC storage        | EF saves `DateTime.UtcNow`; never local in DB                            |
| Display timezone   | `TimeZoneInfo.FindSystemTimeZoneById("America/Denver")` in UI/formatters |
| Soft-delete tenant | `Tenant.IsDeleted`; excluded from default search                         |
| Soft-delete lease  | `Lease.IsDeleted`; retained for history and audit                        |
| Documents          | File bytes on NAS only; DB holds metadata + `FilePathOnNas`              |
| Ledger balance     | Sum(Charges) − Sum(Payments) per tenant+unit                             |
| Audit              | `AuditLog` insert-only; no update/delete                                 |
| NAS paths          | Relative to `/docs` mount ≡ `/volume1/apartments/docs`                   |

---

## NAS Path Conventions

```text
/volume1/apartments/docs/   (host)  →  /docs/   (container)
├── uploads/{unitNumber}/{category}/{filename}
├── leases/{unitNumber}/{leaseId}.pdf
├── appliances/{unitNumber}/{assetId}/{filename}
├── templates/brookside-*.docx|.pdf   (legal masters + fillable AcroForm)
└── photos/units/{unitNumber}/{filename}
```

`Document.FilePathOnNas` stores path relative to `/docs` (e.g. `uploads/12/lease/foo.pdf`).

---

## External Integration

- **PaymentPortalUrl** — deep link only (v1)
- **E-sign** — export PDF; integration post-v1
- **Payment receipt PDF** — v1.1 (NV-1): generate from `LedgerEntry` Payment for print/email; optional Document vault copy

---

## Indexes

- Unit.Number (unique)
- Asset (UnitId, Serial)
- Tenant (LastName) WHERE IsDeleted = false
- Lease (End, Status) WHERE IsDeleted = false
- LedgerEntry (TenantId, UnitId, Date)
- Document (EntityType, EntityId)
- AuditLog (EntityType, EntityId, Timestamp)
