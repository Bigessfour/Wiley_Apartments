# Dashboard full-repo audit (2026-08-11)

Source of truth used: **GitHub `Bigessfour/Wiley_Apartments@master`** (full tree, 195 tracked files).  
GitHub connector identity: `Bigessfour`. No separate `stephenmckitrick` GitHub user/repo is visible to this token.

## Domain that actually exists today

| Entity | In Domain/DbContext | Used by dashboard |
|--------|---------------------|-------------------|
| Unit | Yes | Occupancy, status mix, portfolio |
| Asset (+ WarrantyEnd) | Yes | Warranty alerts (90-day window) |
| Flooring | Yes | Not on home (unit detail only) |
| AuditLog | Yes | No |
| **Tenant** | Spec only — **no class/table** | Phase 2 stub page |
| **Occupancy / Lease** | Spec only | Deferred |
| **LedgerEntry / Payment** | Spec only | Deferred delinquencies |
| **MaintenanceRequest** | Spec only | Deferred work orders |
| **Document** | Spec only | T6.1 |

`Unit.CurrentTenantId` is reserved on the unit model but has **no FK target** yet.

## T6.2 “done when” vs reality

| Requirement | Status |
|-------------|--------|
| SfDashboardLayout landing | Done |
| Live data for units | Done (seeded portfolio) |
| Widgets clickable to detail | Done (unit + warranty links; KPI filters) |
| Occupancy | Live |
| Warranty expirations | Live (assets) |
| Lease expirations | **Blocked** — no Lease |
| Open work orders | **Blocked** — no MaintenanceRequest |
| Delinquencies | **Blocked** — no ledger |
| < 3 s LAN | Expected (single snapshot query) |

## What “finish dashboard” means without inventing Phase 2–4

1. Professional Syncfusion presentation (SfGrid for portfolio + warranties).  
2. Visual occupancy + mix bars from live unit status counts.  
3. Maintenance unit KPI (live status count — not work-order tickets).  
4. Honest “Coming modules” strip mapping to Spec Kit phases.  
5. Do **not** fake lease/WO/$ metrics.

## Next product work that unlocks remaining KPIs

1. **T2.1** Tenant entity + CRUD  
2. **T2.2** Occupancy linking  
3. **T3** Lease + ledger → expirations + delinquencies  
4. **T4** MaintenanceRequest → open WO count  
5. **T6.3** Exportable rent roll / occupancy / warranty reports  
