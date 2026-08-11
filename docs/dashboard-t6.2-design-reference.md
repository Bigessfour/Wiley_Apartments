# T6.2 Clerk home dashboard — design reference

## Goal
Professional data-viz landing page for 16-unit Town of Wiley portfolio using **Syncfusion only** (`SfDashboardLayout`, `SfGrid`, existing Fluent2 theme).

## Layout (12-column, non-editable)
| Row | Panels |
|-----|--------|
| 0 | KPI strip: Occupancy · Vacant/Make-Ready · Open work orders · Delinquent (4× SizeX=3) |
| 1 | Portfolio mix (SizeX=4) + Warranty alerts 90d (SizeX=8) |
| 3 | Unit portfolio grid (SizeX=12) |

- `AllowDragging=false`, `AllowResizing=false`
- `MediaQuery="max-width: 768px"` for mobile stack
- Unique panel `Id`s required

## Data sources (current domain)
| Metric | Source |
|--------|--------|
| Occupancy / status mix | `Unit.Status` |
| Warranty alerts | `Asset.WarrantyEnd` within 90 days of MT “today” |
| Unit portfolio | `Unit` list |
| Work orders / delinquencies / lease expirations | **Deferred** — show 0 until domain exists |

## Blazor ports
- `IDashboardService` / `DashboardService`
- `Components/Pages/Home.razor`
- Unit detail KPI strip → same layout component
- Package: `Syncfusion.Blazor.Layouts` 34.2.2

## Interactive React prototype (Grok App Builder)
Visual IA prototype (not production stack): KPI hierarchy, charts, rent roll, unit detail strip. Use for UX sign-off; production remains Blazor Interactive Server on Synology.


## Closeout UX (post-merge)

- Clickable KPIs and portfolio mix rows filter the unit portfolio
- Skeleton loading + Refresh
- Deferred metrics show badges (not fake data)
- See `docs/dashboard-closeout.md`
