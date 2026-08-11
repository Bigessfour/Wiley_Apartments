# Syncfusion Blazor API review — ClerkSuite home dashboard

**Packages:** Syncfusion.Blazor.* **34.2.2**  
**Page:** `Components/Pages/Home.razor`  
**Reviewed against:** Syncfusion Blazor docs (Dashboard Layout, Circular Gauge, Linear Gauge, Charts, Accumulation Chart, HeatMap, Chart3D, Sparkline, Cards, Buttons) + prior green CI API fixes on this repo.

## Component configuration matrix

| Control | Key props (as implemented) | API notes / corrections applied |
|---------|---------------------------|----------------------------------|
| **SfDashboardLayout** | `ID`, `Columns=6`, `CellSpacing={14,14}`, `AllowDragging/AllowResizing` bound to layout-edit mode, `EnablePersistence=true`, `MediaQuery=max-width: 768px`, `MinSizeX/Y` on panels | Persistence requires stable `ID`. Reset via `ResetPersistDataAsync` + `RefreshAsync`. Drag/resize **off by default** (production-safe); Edit layout unlocks. CellAspectRatio left default to avoid fighting persisted sizes. |
| **DashboardLayoutPanel** | `Id`, `SizeX/Y`, `Row`, `Column`, `MinSizeX/Y`, Header/Content templates | Unique `Id` required for persistence. Charts should fill panel (`height/width 100%`). |
| **SfCircularGauge** | `Theme`, axis 210→150°, `PointerType.RangeBar`, `Value`, annotation | Matches Circular Gauge range-bar pattern. Major/minor ticks hidden for KPI chrome. |
| **SfLinearGauge** | `PointerValue`, `Point.Bar`, axis min/max | Use **`PointerValue`** (not `Value`) for bar pointer—aligned with Syncfusion Linear Gauge pointer API used in this codebase. |
| **SfSparkline** | `TValue`, `DataSource`, `XName`/`YName`, `SparklineType.Area`, `Theme` | Package `Syncfusion.Blazor.Sparkline`; namespace `Syncfusion.Blazor.Sparklines`. |
| **SfAccumulationChart** | `AccumulationType.Pie` + `InnerRadius=55%` (donut look), smart labels, legend bottom | Prefer Pie+InnerRadius (known-good) over enum values that differ by package version. |
| **SfChart** | Category X, `LabelFormat=C0`, zoom (selection/wheel/pinch/scrollbar), export PNG | Zoom settings match Charts zoom docs. Export via `ExportAsync(ExportType.PNG, fileName)`. |
| **SfHeatMap** | JSON cell adaptor: `XDataMapping=Unit`, `YDataMapping=Month`, `ValueMapping=Value`, gradient palette | Cell mode requires `IsJsonData=true` + `AdaptorType.Cell` (per HeatMap working-with-data docs). |
| **SfChart3D** | `EnableRotation`, `RotationAngle`, `TiltAngle`, `Depth`, Category X + `LabelRotationAngle` | Uses Chart3D angle APIs previously corrected in this repo (`LabelRotationAngle`, not 2D-only names). |
| **SfCard / SfButton** | Fluent KPI chrome; outline toolbar buttons | Cards are click targets for navigation (CSS `kpi-card--link`). |
| **Theme** | `Theme.Fluent2` / `Fluent2Dark` from `ClerkSuiteTheme.isDarkMode` | Keep chart Theme in sync with app theme after first render. |

## Deliberate non-goals / constraints

1. **No parallel DbContext queries** from Blazor scope for snapshot+P/L (EF Core contexts are not thread-safe).  
2. **Layout unlocked only on demand**—Syncfusion allows continuous drag/resize; clerks benefit from locked default.  
3. **Export** covers the two primary 2D chart refs (collection + portfolio series); gauges/heatmap/3D not exported in v1 closeout.

## Residual watch items

- After layout drag, some chart hosts may need `RefreshAsync` if panels reflow oddly on rare browsers—monitor clerk feedback.  
- Heatmap with large unit counts: payload is O(units×12); fine for Wiley scale.  
- Node 20 action deprecation warnings in CI are platform hygiene, not component config.
