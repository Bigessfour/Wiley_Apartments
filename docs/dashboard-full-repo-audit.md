# Dashboard full-repo audit (current master)

**As of:** 2026-08-11 · post phase2 merge + closeout  
**Source of truth:** `Bigessfour/Wiley_Apartments@master`

## Product surface (live)

Home `/` is a multi-domain clerk console:

| Feed | Source | UI |
|------|--------|-----|
| Occupancy / vacant / make-ready / maintenance unit counts | `Unit` (`!IsFacility`) | KPI cards + status doughnut |
| Collection this month / rate / 12-mo series / heatmap | `LedgerEntry` payments (deposits excluded) | KPI + gauges + charts + heatmap |
| Outstanding / delinquencies | `IRentRollService.GetDelinquencyAsync` | KPI + list |
| Open work orders | `IMaintenanceService` | KPI + list → `maintenance?unitId=` |
| Lease expirations 30 / 31–60 | `ILeaseService.GetExpiringWithinAsync` | Lists → `/leases/{id}` |
| Schedule reminders (14d) | `IScheduleService` | List → `/schedule` |
| Warranties (90d) | `Asset.WarrantyEnd` | List → `/units/{id}` |
| P/L YTD | `IPortfolioProfitLossService` | Chart3D + series + `/reports/profit-loss` |

Related pages (not stubs): Tenants, Leases, Maintenance, Payments, Reports, Schedule, Documents, Community Center.

## Spec status

T6.2 / T6.3 / T6.4 and viz NV-5 / NV-6 are implemented. Closeout work is ops polish (loading, as-of, layout lock, deep links, tests, Syncfusion config hygiene)—not greenfield rebuild.

## Performance notes

`ApartmentsDbContext` is scoped and **not thread-safe**. Snapshot queries stay sequential on one context; payment series/heatmap/current-month share **one** ledger read (`BuildPaymentAggregatesAsync`).

## Stale docs

Earlier thin-`master` notes that claimed “no Tenant/Lease/WO domain” are **obsolete**. Prefer this file and `docs/handover/SPECKIT-DONE.md`.
