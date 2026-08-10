# Service Contracts: ClerkSuite v1

Interfaces for `Wiley.Apartments.Contracts` — maps to FR-1 through FR-7.

## IUnitService (FR-1)

```csharp
Task<IReadOnlyList<UnitSummary>> GetAllAsync(CancellationToken ct = default);
Task<UnitDetail?> GetByIdAsync(Guid id, CancellationToken ct = default);
Task<UnitDetail> CreateAsync(CreateUnitRequest request, CancellationToken ct = default);
Task<UnitDetail> UpdateAsync(UpdateUnitRequest request, CancellationToken ct = default);
Task<IReadOnlyList<ApplianceAsset>> GetAppliancesAsync(Guid unitId, CancellationToken ct = default);
Task<ApplianceAsset> SaveApplianceAsync(SaveApplianceRequest request, CancellationToken ct = default);
Task<CarpetRecord> SaveCarpetAsync(SaveCarpetRequest request, CancellationToken ct = default);
```

## ITenantService (FR-2)

```csharp
Task<IReadOnlyList<TenantSummary>> SearchAsync(string query, bool includeDeleted, CancellationToken ct = default);
Task<TenantDetail?> GetByIdAsync(Guid id, CancellationToken ct = default);
Task<TenantDetail> CreateAsync(CreateTenantRequest request, CancellationToken ct = default);
Task<TenantDetail> UpdateAsync(UpdateTenantRequest request, CancellationToken ct = default);
Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
Task StartOccupancyAsync(StartOccupancyRequest request, CancellationToken ct = default);
Task EndOccupancyAsync(EndOccupancyRequest request, CancellationToken ct = default);
Task<IReadOnlyList<OccupancyHistoryEntry>> GetOccupancyHistoryAsync(Guid tenantId, CancellationToken ct = default);
```

## ILeaseService (FR-3)

```csharp
Task<IReadOnlyList<LeaseTemplateInfo>> ListTemplatesAsync(CancellationToken ct = default);
Task<LeaseDraft> CreateDraftAsync(Guid unitId, Guid tenantId, Guid templateId, CancellationToken ct = default);
Task<LeasePreview> PreviewAsync(Guid draftLeaseId, CancellationToken ct = default);
Task<LeaseDetail> FinalizeAsync(Guid draftLeaseId, CancellationToken ct = default);
Task<LeaseDetail> RenewAsync(RenewLeaseRequest request, CancellationToken ct = default);
Task<LeaseDetail> AmendAsync(AmendLeaseRequest request, CancellationToken ct = default);
Task<LeaseDetail> TerminateAsync(TerminateLeaseRequest request, CancellationToken ct = default);
Task<IReadOnlyList<LeaseSummary>> GetExpiringAsync(int withinDays, CancellationToken ct = default);
Task<byte[]> ExportForESignAsync(Guid leaseId, CancellationToken ct = default);
```

## ILedgerService (FR-4)

```csharp
Task<LedgerEntry> PostChargeAsync(PostChargeRequest request, CancellationToken ct = default);
Task<LedgerEntry> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken ct = default);
Task<IReadOnlyList<LedgerEntry>> GetLedgerAsync(Guid tenantId, Guid unitId, CancellationToken ct = default);
Task<decimal> GetBalanceAsync(Guid tenantId, Guid unitId, CancellationToken ct = default);
Task ApplyLateFeesAsync(DateOnly asOf, CancellationToken ct = default);
Task<RentRollReport> GenerateRentRollAsync(CancellationToken ct = default);
Task<DelinquencyReport> GenerateDelinquencyReportAsync(CancellationToken ct = default);
string GetPaymentPortalUrl(Guid tenantId, Guid unitId);
```

## IDocumentService (FR-5)

```csharp
Task<IReadOnlyList<DocumentInfo>> ListAsync(DocumentListQuery query, CancellationToken ct = default);
Task<DocumentInfo> UploadAsync(UploadDocumentRequest request, Stream content, CancellationToken ct = default);
Task<Stream> DownloadAsync(Guid documentId, CancellationToken ct = default);
Task<DocumentViewInfo> GetViewInfoAsync(Guid documentId, CancellationToken ct = default);
```

## IDashboardService (FR-6)

```csharp
Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken ct = default);
Task<OccupancyReport> GetOccupancyReportAsync(CancellationToken ct = default);
Task<MaintenanceCostReport> GetMaintenanceCostByUnitAsync(DateRange range, CancellationToken ct = default);
Task<WarrantyStatusReport> GetWarrantyStatusAsync(CancellationToken ct = default);
```

## IMaintenanceService

```csharp
Task<MaintenanceDetail> CreateAsync(CreateMaintenanceRequest request, CancellationToken ct = default);
Task<MaintenanceDetail> UpdateStatusAsync(Guid id, UpdateMaintenanceStatusRequest request, CancellationToken ct = default);
Task<IReadOnlyList<MaintenanceSummary>> GetForUnitAsync(Guid unitId, CancellationToken ct = default);
Task<IReadOnlyList<MaintenanceSummary>> GetForAssetAsync(Guid assetId, CancellationToken ct = default);
```

## IAuditService (FR-7)

```csharp
Task<IReadOnlyList<AuditLogInfo>> QueryAsync(AuditQuery query, CancellationToken ct = default);
```

## ICurrentUserService

```csharp
string UserId { get; }
string DisplayName { get; }
bool IsClerk { get; }
bool IsReadOnly { get; }
bool IsElevated { get; }
```

## Authorization

- Write: `Clerk` or `Elevated`
- Read: `Clerk`, `ReadOnly`, `Elevated`
- Audit viewer: `Clerk`, `Elevated`

## Configuration keys

- `PaymentPortalUrl` — external town card portal (FR-016)
- `DocumentRoot` — container mount for `/volume1/apartments/docs`
- `MaxUnits` — `0` = unlimited residential (default); positive = hard cap

## Next version (v1.1) — not in v1 contracts

- **Payment receipt PDF** from a Payment `LedgerEntry` (print / download / email workflow) — see plan.md § Next version (NV-1).
