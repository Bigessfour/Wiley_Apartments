# Dashboard closeout checklist (T6.2)

**PR base:** merged T6.2 (`feature/t6.2-dashboard-dataviz` → `master`)  
**Closeout branch:** `feature/dashboard-closeout`

## Closed in this pass

| Item | Status |
|------|--------|
| SfDashboardLayout home + unit detail KPI strip | Done (merged) |
| Live occupancy / status / warranties | Done (merged) |
| Unit portfolio deep links | Done (merged) |
| KPI / mix click → portfolio filter | Done |
| Filter chips (All + statuses) | Done |
| Loading skeleton + Refresh / Retry | Done |
| Deferred badges for WO / delinquencies | Done |
| Warranty urgency highlight (≤30 days) | Done |
| E2E: unauthenticated `/` → login | Done |
| SfGrid portfolio + warranty grids | Done (finish pass) |
| Occupancy / mix visual bars | Done (finish pass) |
| Maintenance unit KPI (live status) | Done (finish pass) |
| Coming-modules strip (honest phase map) | Done (finish pass) |
| Full-repo audit | `docs/dashboard-full-repo-audit.md` |
| Faster CI (Ollama on failure) | Done (merged) |

## Explicitly deferred (domain-gated)

| Item | Blocker |
|------|---------|
| Lease expiration panel | Lease entity / service |
| Live work-order KPI | Maintenance module |
| Live delinquent $ | Payments / balances |
| Rent roll export (T6.3) | Reports task |
| SfChart donut for portfolio mix | Optional; package + license |

## Manual smoke (NAS / local)

1. Sign in → `/` shows KPIs and unit table within a few seconds  
2. Click **Occupied** KPI or mix row → portfolio filters  
3. **Vacant / Make-Ready** KPI → combined filter  
4. **Clear filter** / **All** restores full list  
5. Open unit from portfolio or warranty row  
6. **Refresh** reloads snapshot timestamp  
