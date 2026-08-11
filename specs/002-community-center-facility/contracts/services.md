# Contracts: Community Center Facility Services (002)

Service interfaces live in `src/Wiley.Apartments.Contracts`. Implementations in `src/Wiley.Apartments.Web/Services`. All mutating methods MUST write `AuditLog`.

---

## IFacilityRenterService

```csharp
Task<IReadOnlyList<FacilityRenter>> SearchAsync(string? query, bool includeDeleted = false, CancellationToken ct = default);
Task<FacilityRenter?> GetByIdAsync(Guid id, CancellationToken ct = default);
Task<FacilityRenter> CreateAsync(FacilityRenter renter, CancellationToken ct = default);
Task<FacilityRenter> UpdateAsync(FacilityRenter renter, CancellationToken ct = default);
Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
```

**Rules**: Soft-delete fails if future `Request`/`Confirmed` reservations exist.

---

## IFacilityReservationService

```csharp
Task<IReadOnlyList<FacilityReservation>> ListAsync(Guid? unitId, FacilityReservationStatus? status, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
Task<FacilityReservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
Task<FacilityReservation> CreateAsync(FacilityReservation reservation, CancellationToken ct = default);
Task<FacilityReservation> UpdateAsync(FacilityReservation reservation, CancellationToken ct = default);
Task<FacilityReservation> SetStatusAsync(Guid id, FacilityReservationStatus status, string? note, CancellationToken ct = default);
Task EnsureNoConfirmedOverlapAsync(Guid unitId, DateTime startUtc, DateTime endUtc, Guid? excludeId, CancellationToken ct = default);
```

**Rules**:
- `UnitId` must reference `IsFacility` unit.
- Transition to `Confirmed` runs overlap check and upserts `ScheduledItem` (category FacilityRental).
- Cancel clears or marks related ScheduledItem completed/cancelled per implementation note.
- Completed typically requires PostRental inspection recorded (warn or require — **require** satisfactory or explicit override note).

---

## IFacilityRentalAgreementService

```csharp
Task<FacilityAgreementResult> GenerateAsync(Guid reservationId, CancellationToken ct = default);
Task AttachSignedAsync(Guid reservationId, Stream pdf, string fileName, string uploadedBy, CancellationToken ct = default);
```

`FacilityAgreementResult`: reservationId, pdf bytes, relative path, documentId.

---

## IFacilityInspectionService

```csharp
Task<IReadOnlyList<FacilityInspection>> ListForReservationAsync(Guid reservationId, CancellationToken ct = default);
Task<IReadOnlyList<FacilityInspection>> ListRecentAsync(int take, CancellationToken ct = default);
Task<FacilityInspection> CreateAsync(FacilityInspection inspection, CancellationToken ct = default);
Task<FacilityInspection> UpdateAsync(FacilityInspection inspection, CancellationToken ct = default);
```

**Rules**: `!IsSatisfactory` ⇒ `DamageNotes` required.

---

## IFacilityInventoryService

```csharp
Task<IReadOnlyList<FacilityInventoryItem>> ListAsync(Guid unitId, FacilityInventoryCategory? category, CancellationToken ct = default);
Task<FacilityInventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
Task<FacilityInventoryItem> CreateAsync(FacilityInventoryItem item, CancellationToken ct = default);
Task<FacilityInventoryItem> UpdateAsync(FacilityInventoryItem item, CancellationToken ct = default);
Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
```

---

## Maintenance / Schedule / Ledger extensions

| Service                                     | Additive contract                                                               |
| ------------------------------------------- | ------------------------------------------------------------------------------- |
| `IMaintenanceService.CompleteAsync`         | Add `completedByDisplay` (and optional user id) parameters                      |
| `IScheduleService`                          | Persist `FacilityReservationId`; filter helpers for CC                          |
| `ILedgerService` (or existing payment APIs) | Create charge/payment with `FacilityReservationId` + `FacilityRenterId`         |
| `IPaymentReceiptService`                    | Accept facility-linked payment entries; receipt title “Community Center rental” |

---

## UI routes (authorized)

| Route                                 | Page                                               |
| ------------------------------------- | -------------------------------------------------- |
| `/community-center`                   | Hub (expanded)                                     |
| `/community-center/renters`           | Renter list                                        |
| `/community-center/renters/{id}`      | Renter detail                                      |
| `/community-center/reservations`      | Reservation list                                   |
| `/community-center/reservations/{id}` | Reservation detail (agreement, money, inspections) |
| `/community-center/inspections`       | Inspection list                                    |
| `/community-center/inventory`         | Inventory grid                                     |
| `/schedule?unitId={cc}`               | Shared calendar filtered                           |
| `/payments?unitId={cc}`               | Ledger filtered                                    |
| `/maintenance?unitId={cc}`            | WO list filtered                                   |

All pages: Syncfusion components only; Interactive Server.
