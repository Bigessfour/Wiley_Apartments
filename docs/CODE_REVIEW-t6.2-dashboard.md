> **Superseded for current master:** see `docs/dashboard-full-repo-audit.md` and `docs/dashboard-closeout.md` (phase2 product is on master).

# Code review — T6.2 dashboard data viz

**Branch:** `feature/t6.2-dashboard-dataviz`  
**Date:** 2026-08-11  
**Scope:** Dashboard service, Home landing, UnitDetail KPI strip, Layouts package, tests

## Summary
Implements Spec Kit **T6.2** shell with live occupancy/status and warranty alerts from existing domain. Delinquencies, work orders, and lease expirations intentionally zeroed with UI copy until those modules land. Unit detail summary cards upgraded to `SfDashboardLayout` for visual consistency.

## Checklist

| Area | Result | Notes |
|------|--------|-------|
| Spec alignment | Pass (partial) | Live data for units/assets; deferred metrics called out in UI |
| Syncfusion only | Pass | Layouts + Grid + Cards; no new chart libs |
| DI registration | Pass | `IDashboardService` → `DashboardService` scoped |
| Package version | Pass | Layouts **34.2.2** matches other Syncfusion refs |
| Auth | Pass | Home remains `[Authorize]` |
| Performance | Pass (expected) | Single snapshot; asset warranty query filtered + Take(25) |
| Tests | Pass (added) | Occupancy math + warranty window |
| Accessibility | Partial | Panel headers present; color not sole status signal on unit detail badges |
| Mobile | Pass (config) | MediaQuery stack at 768px |
| Security | Pass | No PII beyond existing unit notes; no secrets |

## Findings

### Must fix before merge
- None blocking if CI green.

### Should fix soon
1. **Grid `Template` context cast** — Syncfusion Blazor template typing can be fragile; if RZ compile warns, switch to strongly typed `GridColumn` template components.
2. **Timezone** — Snapshot “today” uses `IDateTimeService.ToDisplayTime` then `DateOnly` — confirm MT conversion matches warranty clerk expectations (tests use identity clock).
3. **Lease expirations panel** — Spec mentions expirations; omitted until lease entity exists. Track under G2/leases.

### Nice to have
- `SfChart` accumulation chart for portfolio mix (add `Syncfusion.Blazor.Charts` when license tier allows).
- Clickable KPI filters on unit portfolio (status filter).
- Skeleton panels while loading.

## Test plan (CI / local)
```bash
dotnet test Wiley.Apartments.slnx --filter FullyQualifiedName~DashboardServiceTests
dotnet build Wiley.Apartments.slnx
# Manual: sign in → `/` shows KPIs; open unit details uses layout strip; warranties link to `/units/{id}`
```

## Risk
Low. Additive service + UI; no migration. Deferred metrics display zero rather than inventing data.
