

# Tasks: ClerkSuite — Wiley Apartment Management v1

**Input**: [spec.md](./spec.md) · [plan.md](./plan.md) · [data-model.md](./data-model.md) · [quickstart.md](./quickstart.md)

**Repository**: [github.com/Bigessfour/Wiley_Apartments](https://github.com/Bigessfour/Wiley_Apartments)

**Format**: `T{phase}.{item}` — each task includes **Done when** acceptance tied to clerk outcomes.

---

## Phase 0 — Project Scaffolding & Infrastructure

- [x] **T0.0** Syncfusion toolchain verification
  - Install/confirm **Blazor UI Builder skill** for Cursor: `apm install syncfusion/blazor-ui-builder -t cursor` — **done 2026-08-09**
  - Install/confirm **Blazor MCP server** and **component skills**:
    - MCP: `sf-blazor-mcp` in `~/.cursor/mcp.json` → `@syncfusion/blazor-assistant` via `run-sf-blazor-mcp.sh` — **verified**
    - Skills: `npx skills add syncfusion/blazor-ui-components-skills -g` — **67 skills installed**
  - Confirm **license key** and **API key** via Keychain → secure env (see [READINESS.md §8](./READINESS.md)):
    - License: Keychain `SYNCFUSION_LICENSE_KEY` / `SYNCFUSION` — **found**
    - MCP API: Keychain bridge via `run-sf-blazor-mcp.sh` — **verified** (`sf_blazor_assistant` responded)
  - **Done when:** Agentic UI Builder and MCP respond correctly in Cursor — **pass**; minimal Syncfusion component without watermark — **completes at T0.1 first build**; `git grep` shows no committed key values — **pass**
  - **Evidence:** [deploy/synology/SYNCFUSION-SECRETS.md](../../../deploy/synology/SYNCFUSION-SECRETS.md) verification log

- [x] **T0.1** Initialize Spec Kit project / solution structure (Blazor Interactive Server Web App).
  - **Done when:** Solution builds; Syncfusion NuGet referenced; license from user-secrets; `SfSidebar`/`SfButton` shell — **2026-08-09**
  - **Paths:** `Wiley.Apartments.slnx`, `src/Wiley.Apartments.Web/`, `scripts/setup-local-secrets.sh`

- [x] **T0.2** Create Dockerfiles + docker-compose.yml suitable for Synology Container Manager.
  - **Done when:** `deploy/Dockerfile`, `deploy/docker-compose.yml`, `deploy/docker-compose.postgres.yml` (override only) — **2026-08-09**

- [x] **T0.3** Set up NAS shared folder for documents and volume mounts.
  - **Done when:** NAS reachable via **Tailscale + SSH**; container read/write test on `/volume1/apartments/docs` — **2026-08-09**
  - **Evidence:** share created on `mr-storage`; `scripts/verify-documents-mount.sh nas` PASS; local mode via `./local-docs`
  - **Paths:** `deploy/synology/README.md`, `.env.sample`, `scripts/verify-documents-mount.sh`

- [x] **T0.4** Basic auth + AuditLog table (no role differentiation).
  - **Done when:** Identity login, full access (no roles), AuditLog + interceptor — **2026-08-09** (seed users via `SeedUsers` config at deploy)

- [x] **T0.5** RAM / resource check and documentation for DS225+.
  - **Done when:** `deploy/synology/DEPLOY.md`, `deploy/synology/RESOURCE-NOTES.md` — **2026-08-09**

- [x] **T0.6** UTC storage + America/Denver display helpers.
  - **Done when:** `IDateTimeService` + dashboard display — **2026-08-09**; `.env.sample` includes `TZ=America/Denver`

- [x] **T0.7** Serilog structured logging + test pyramid (unit / integration / E2E).
  - **Done when:** Serilog console + rolling file (`logs/clerksuite-.log`); request logging enrichers; `tests/` projects cover current services/infrastructure with unit (10), integration (6), E2E Playwright+HTTP (3) — **2026-08-09**
  - **Policy:** new services ship with tests in all three layers (see [tests/README.md](../../../tests/README.md))

**Checkpoint:** Phase 0 complete — **T0.0 toolchain verified**; app runs in Docker; auth + audit + NAS I/O proven; logging + tests in place.

---

## Phase 1 — Core Domain (Units + Assets)

- [x] **T1.1** Unit CRUD + status management + unique attributes (sq ft, beds, baths, notes).
  - **Seed:** **Unit 1–16 placeholders** (Town of Wiley, CO) until real list supplied (G1).
  - **Done when:** All **16 units** can be entered and edited; status changes (Occupied / Vacant / Maintenance / Make-Ready) visible; cap enforced at 16 — **2026-08-09**
  - **Paths:** `Domain/Unit.cs`, `Services/UnitService.cs`, `Pages/Units/UnitList.razor`
  - **UI:** Built with `sf_blazor_assistant` guidance (SfGrid inline CRUD + status dropdown)

- [x] **T1.2** Asset/Appliance inventory (make, model, serial, install, warranty, photos, attachments).
  - **Done when:** Assets nested under units; searchable by serial; warranty start/end stored; manuals/receipts attach via Document entity — **2026-08-09** (Document attach deferred to T6.1; `PhotoPaths` field ready)
  - **Paths:** `Domain/Asset.cs`, `Services/AssetService.cs`, `Pages/Units/UnitDetail.razor` (assets tab)

- [x] **T1.3** Flooring/carpet records linked to units.
  - **Done when:** Install date, type/material, condition, replacement history captured and shown on unit detail — **2026-08-09**
  - **Paths:** `Domain/Flooring.cs`, `Services/FlooringService.cs`, `Pages/Units/UnitDetail.razor` (flooring tab)

- [x] **T1.4** Unit detail page with Syncfusion DataGrid / cards for assets and history.
  - **Done when:** Clerk opens any unit and sees complete inventory + status in one view (`SfGrid` + cards) — **2026-08-09**
  - **Compliance:** UI built/refined via `sf_blazor_assistant`; no non-Syncfusion table/grid controls.
  - **Paths:** `Pages/Units/UnitDetail.razor`, `Pages/Units/UnitList.razor`

**Checkpoint:** Phase 1 complete — **FR-1 acceptance criteria pass** (2026-08-09).

---

## Phase 2 — Tenants & Occupancy

- [x] **T2.1** Tenant full CRUD + household members + contacts (vehicles, pets, emergency).
  - **Done when:** Create / edit / search / **soft-delete** works; screening documents attachable to tenant.
  - **Paths:** `Domain/Tenant.cs`, `ITenantService` / `TenantService.cs`, `Pages/Tenants/TenantList.razor`, `Pages/Tenants/TenantDetail.razor` (household/vehicles/pets), migration `AddTenants`
  - **Tests:** unit (`TenantServiceTests`), integration (`TenantIntegrationTests`), E2E auth redirect (`TenantsPageE2ETests`)
  - **Progress (2026-08-09):** CRUD + soft-delete + search + nested edit/update for household/vehicles/pets (incl. DOB) on PR `#2`. Screening document attach **deferred** to Document vault (Phase 6 / T6.x).

- [x] **T2.2** Occupancy linking (start/end) with history.
  - **Done when:** Current tenant on unit detail; past occupancy retained and viewable; unit status updates on start/end.
  - **Paths:** `Domain/Occupancy.cs`, `IOccupancyService` / `OccupancyService.cs`, migration `AddOccupancies`, `Pages/Units/UnitDetail.razor` (Occupancy tab), history on `Pages/Tenants/TenantDetail.razor`
  - **Progress (2026-08-09):** Start/end sets `Unit.Status` Occupied↔Vacant and `CurrentTenantId`; history retained; unit + tenant views show history. Tests: `OccupancyServiceTests`.

- [x] **T2.3** Tenant detail page (related records).
  - **Done when:** Related leases, payments/ledger, and documents accessible from tenant view.
  - **Paths:** `Pages/Tenants/TenantDetail.razor`
  - **Progress (2026-08-09):** Leases grid + open/new links (`ILeaseService.GetByTenantAsync`). Payments/ledger: portal deep-link stub (Phase 4 fills ledger). Documents: Phase 6 stub note; lease PDFs via Leases section.

**Checkpoint:** FR-2 core (CRUD + occupancy + related-record navigation) **pass**; screening attach deferred to Phase 6; ledger/doc vault content wait on Phases 4/6.

---

## Phase 3 — Leases

- [x] **T3.1** Lease entity + key-date tracking + status (Active, Expired, Terminated, etc.).
  - **Done when:** Leases created and linked to unit + tenant; **soft-delete** retains history; `TemplateUsed` recorded.
  - **Paths:** `Domain/Lease.cs`, `Services/LeaseService.cs`
  - **Progress (2026-08-09):** `Lease` + `LeaseStatus`, EF migration `AddLeases`, `ILeaseService`/`LeaseService` (draft, soft-delete, list).

- [x] **T3.2** Lease generator from template (populate unit/tenant data).
  - **Done when:** Clerk generates filled PDF from Brookside fillable templates (AcroForm) with unit/tenant data; optional custom-clause addendum page; preview via SfPdfViewer2. Source blanks under `DocumentRoot/templates/brookside-*.docx` (PDF siblings auto-bootstrapped).
  - **Paths:** `Pages/Leases/LeaseWizard.razor`, `Pages/Leases/LeasePreview.razor`, `Services/LeaseDocumentGenerator.cs`
  - **Progress (2026-08-09):** **PDF-first** — `Syncfusion.Pdf.Net.Core` + `Syncfusion.Blazor.SfPdfViewer`. See `deploy/synology/TEMPLATES.md`.

- [x] **T3.3** Upload/link signed lease + document vault integration.
  - **Done when:** Signed document on NAS (`/docs/leases/...`) and linked to lease via Document entity; status can move Draft → Active.
  - **Paths:** `Services/DocumentService.cs`, lease detail upload
  - **Progress (2026-08-09):** `Document` entity + migration `AddDocumentsAndSignedLease`; `IDocumentService` vault upload; `AttachSignedDocumentAsync` → `leases/{unit}/signed/…`, `SignedDocumentId`, status **Active**; SfUploader on `LeasePreview`.

- [x] **T3.4** Renew / amend / terminate workflows.
  - **Done when:** Status and history update correctly; dashboard expiration feed reflects changes.
  - **Paths:** `Services/LeaseService.cs` (Renew/Amend/Terminate)
  - **Done (2026-08-09):** `AmendAsync` / `RenewAsync` / `TerminateAsync` + `GetExpiringWithinAsync`; Prior/Successor/LifecycleNote; Lease preview lifecycle panels. Dashboard expiration feed remains **T6.2**.

**Checkpoint:** FR-3 acceptance criteria pass.

---

## Phase 3.5 — Operations Calendar (Scheduler)

Clerk schedule for unit-linked date work (cleaning, vacancy, inspections, reminders / action items). Placed after leases so entries can optionally link to lease/vacancy dates; before payments so calendar is not blocked on ledger.

- [x] **T3.5.1** `ScheduledItem` entity + service (unit optional tenant/lease link; categories; due/start/end; reminder offset; completion).
  - **Done when:** CRUD persists; soft-delete; filter by unit/category/date range.
  - **Paths:** `Domain/ScheduledItem.cs`, `IScheduleService` / `ScheduleService.cs`, migration
  - **Done (2026-08-09):** `ScheduledItem` + `ScheduledItemCategory`; `IScheduleService` / `ScheduleService`; migration `AddScheduledItems`; unit tests.

- [x] **T3.5.2** Syncfusion `SfSchedule` UI (`Syncfusion.Blazor.Schedule`) — day/week/month/agenda; resource-by-unit optional.
  - **Done when:** Clerks create/edit/drag items; categories for cleaning / vacancy / inspection / other; reminders surface on dashboard later (T6.2).
  - **Paths:** `Pages/Schedule/OperationsCalendar.razor`, nav link
  - **Done (2026-08-09):** `/schedule` SfSchedule day/week/month/agenda; editor category + unit; drag/resize persist via `IScheduleService`; nav links. Resource-by-unit deferred; reminders on T6.2.

- [x] **T3.5.3** Seed common recurring patterns (e.g. turn-over clean after vacancy) — optional v1 polish.
  - **Done when:** At least vacancy→clean suggestion or template actions documented.
  - **Done (2026-08-09):** `/schedule` **Suggest turnover cleans** for Vacant units; `deploy/synology/SCHEDULE-PATTERNS.md`.

**Checkpoint:** Clerks can track date-bound unit work with reminders independently of maintenance work orders (Phase 5).

---

## Phase 4 — Payments & Ledger

- [x] **T4.1** Charge and Payment entities + running balance ledger.
  - **Done when:** Clerk posts rent charges and payments; running balance accurate per tenant/unit (`LedgerEntry` Charge/Payment types).
  - **Paths:** `Domain/LedgerEntry.cs`, `Services/LedgerService.cs`, `Pages/Payments/`
  - **Done (2026-08-09):** `LedgerEntry` + `ILedgerService` (post charge/payment, running balance, soft-delete); `/payments` SfGrid + post forms; migration `AddLedgerEntries`; unit tests.

- [x] **T4.2** Late-fee settings and assessment (G2).
  - **Done when:** `LateFeesEnabled` settings toggle **default OFF**; when enabled, configurable **amount + grace days**; staff can assess late fees; charges appear on ledger.
  - **Paths:** `Domain/LateFeeSettings.cs`, `Services/LedgerService.ApplyLateFeesAsync`, settings UI
  - **Done (2026-08-09):** `LateFeeSettings` + `ILateFeeSettingsService`; Settings UI toggle/amount/grace + Assess; `ApplyLateFeesAsync` (one/month).

- [x] **T4.3** Deep-link to Town of Wiley PayStar payment portal (G3).
  - **Done when:** Link on tenant/lease view opens `PaymentPortalUrl` (default: townofwiley.gov pay-bill → `secure.paystar.io`); configurable env; no card data in ClerkSuite.
  - **Paths:** tenant/lease detail components, `.env.sample` (`PaymentPortalUrl=`)
  - **Done (2026-08-09):** Tenant detail + lease preview + Settings portal display; `deploy/synology/.env.example` documents `PaymentPortalUrl` / late-fee env keys.

- [x] **T4.4** Rent roll / delinquency report.
  - **Done when:** Report generated and exportable/printable for all **16 units**.
  - **Paths:** `Pages/Reports/RentRoll.razor`, `Pages/Reports/Delinquency.razor`
  - **Done (2026-08-09):** `IRentRollService`; `/reports`, rent-roll + delinquency SfGrids with print.

- [x] **T4.5** Unit operating costs (landlord P&L) — separate from tenant ledger.
  - **Done when:** Clerks can record costs per unit with categories **Utility / Repair / Replace / CommonUpkeep**; `UnitId` nullable only for CommonUpkeep (building-wide); soft-delete; filter by unit/category/date; sum-by-unit helper. Does **not** affect tenant `LedgerEntry` balances.
  - **Paths:** `Domain/UnitOperatingCost.cs`, `Domain/OperatingCostCategory.cs`, `IUnitOperatingCostService` / `UnitOperatingCostService.cs`, migration
  - **Done (2026-08-09):** Entity + service + migration `AddLateFeesAndOperatingCosts`; unit tests.

- [x] **T4.6** Unit ops-cost UI + report (Syncfusion).
  - **Done when:** Unit detail shows ops-cost grid; report by unit/category exportable; optional link from Phase 5 maintenance WO cost → ops cost when available.
  - **Paths:** unit detail tab, `Pages/Reports/OperatingCosts.razor`
  - **Done (2026-08-09):** Unit detail Ops costs tab; `/reports/operating-costs` record + print. Phase 5 WO link deferred.

**Checkpoint:** FR-4 acceptance criteria pass; T4.5–T4.6 add landlord ops-cost tracking for the 16-unit portfolio.

---

## Phase 5 — Maintenance

- [ ] **T5.1** Work-order / maintenance request CRUD linked to unit and optional asset.
  - **Done when:** Requests created; status assigned; cost recorded (`MaintenanceRequest`).
  - **Paths:** `Domain/MaintenanceRequest.cs`, `Services/MaintenanceService.cs`

- [ ] **T5.2** History visible on unit and asset pages.
  - **Done when:** Past maintenance in chronological order with costs on unit detail and asset detail.
  - **Paths:** `Pages/Maintenance/`, unit/asset detail tabs

**Checkpoint:** FR-5 maintenance portions + FR-1 maintenance history pass.

---

## Phase 6 — Documents & Dashboard

- [ ] **T6.1** Document metadata + FileManager / upload to NAS shared folder.
  - **Done when:** Upload, categorize, open via Syncfusion PdfViewer/DocumentEditor; polymorphic `EntityType` + `EntityId`; **fallback** download for oversized files.
  - **Paths:** `Domain/Document.cs`, `Pages/Documents/DocumentBrowser.razor`

- [ ] **T6.2** Clerk home dashboard (occupancy, expirations, open work orders, delinquencies, warranties).
  - **Done when:** SfDashboardLayout default landing page; live accurate data for 16 units; loads **< 3 s** on LAN; widgets clickable to detail.
  - **Paths:** `Pages/Dashboard/Home.razor`, `Services/DashboardService.cs`

- [ ] **T6.3** Basic exportable reports (rent roll, occupancy, warranty list).
  - **Done when:** Rent roll printable/downloadable; occupancy and warranty status reports available.
  - **Paths:** `Pages/Reports/`

- [ ] **T6.4** City council P/L data viz (depends on **T4.1** income + **T4.5** ops costs).
  - **Done when:** Dashboard (or Reports) shows Syncfusion **SfChart** (a) **P/L per apartment** for all 16 units and (b) **monthly and yearly portfolio net income / P/L**; period selector (month/YTD/year); printable/exportable for council review; empty/zero state when no ledger or ops-cost data yet. Does not replace QuickBooks.
  - **Paths:** `Services/PortfolioProfitLossService.cs` (or DashboardService helpers), `Pages/Dashboard/` chart panels and/or `Pages/Reports/PortfolioProfitLoss.razor`; `Syncfusion.Blazor.Charts`

**Checkpoint:** FR-5, FR-6 acceptance criteria pass (including council P/L viz).

---

## Phase 7 — Hardening & Handover

- [ ] **T7.1** End-to-end clerk workflow test on real NAS from both Windows 11 machines.
  - **Done when:** Both clerks complete: create-unit → add-tenant → generate-lease → record-payment → upload-document without errors.
  - **Evidence:** [quickstart.md](./quickstart.md) sign-off table completed.

- [ ] **T7.2** Backup verification (Hyper Backup / snapshots cover data + docs).
  - **Done when:** Documented restore test succeeds (DB volume + `/volume1/apartments/docs`).
  - **Paths:** `deploy/synology/BACKUP-RESTORE.md`

- [ ] **T7.3** User guide / quick-reference for the two clerks (one-pager + screenshots).
  - **Done when:** Guide exists at `docs/clerk-quick-reference.md`; reviewed by at least one clerk.
  - **Paths:** `docs/clerk-quick-reference.md`

- [ ] **T7.4** Final Spec Kit converge / done check.
  - **Done when:** `/speckit.converge` or `speckit-done` skill reports zero Critical/Major gaps against this task list and [spec.md](./spec.md) acceptance criteria.

**Checkpoint:** Project ready for production clerk use.

---

## Overall Definition of Done (Project)

The system is **done** when:

1. All Phase 0–7 tasks above are checked.
2. Every FR acceptance criterion in [spec.md](./spec.md) is satisfied.
3. The two clerks can perform a full daily cycle on the live DS225+ from their Windows 11 PCs.
4. Data and documents survive a NAS reboot and are covered by existing backup processes.
5. [Constitution](../../.specify/memory/constitution.md) principles are respected — especially auditability, NAS data residency, and Syncfusion UI quality.

---

## Dependencies & Execution Order

```text
Phase 0 (blocking)
  → Phase 1 (units/assets)
  → Phase 2 (tenants)
  → Phase 3 (leases, fillable PDF)
  → Phase 3.5 (operations calendar / SfSchedule)
  → Phase 4, 5 (payments + maintenance; can overlap)
  → Phase 6 (dashboard needs 3–5 + calendar reminders)
  → Phase 7 (after 1–6)
```

### MVP milestone (optional early demo)

Phase 0 + Phase 1 + Phase 2 + T0.4 = clerks manage 16 units and tenants with audit.

### Parallel opportunities

- T0.2 and T0.3 in parallel after T0.1
- T1.2 and T1.3 in parallel
- Phase 3 and Phase 4 in parallel after Phase 2 (different developers or sessions)

---

## Notes

- Run `/speckit-implement` starting at **T0.1**
- Use `/speckit.converge` during build if scope grows mid-flight
- Use global `speckit-done` skill at **T7.4**
- Do **not** push to GitHub until full spec-kit pass is complete (project policy)

---

## Phase 8: Convergence

Mid-flight converge (2026-08-09) after fillable-PDF lease pivot + early Document vault. Open work already tasked in T3.4–T7.x is **not** duplicated here.

- [x] **T008** CRITICAL: Enforce AuditLog append-only (reject Update/Delete of `AuditLog` rows in interceptor or EF config) per Constitution III (`missing`)
  - **Done (2026-08-09):** `AuditSaveChangesInterceptor.EnforceAuditLogAppendOnly`; unit tests.

- [x] **T009** Reconcile lease-template artifacts with fillable PDF + SfPdfViewer implementation per plan: Syncfusion surface map / FR-009 (`contradicts`)
  - **Done (2026-08-09):** Updated `plan.md` surface map, `research.md` Decision 7, T3.2 Done-when; `TEMPLATES.md` remains canonical.

- [x] **T010** Add clerk-queryable AuditLog viewer (SfGrid, filter by entity/user/date) per FR-025 / Constitution III / SC-003 (`missing`)
  - **Done (2026-08-09):** `IAuditQueryService` + `/audit` SfGrid; nav links in MainLayout + NavMenu.

- [x] **T011** Support custom lease clauses at generate time per FR-009 (`partial`)
  - **Done (2026-08-09):** `Lease.CustomClauses` + wizard field; PDF addendum page via `AppendCustomClausesIfNeeded`.

- [x] **T012** Add e-signature readiness stub per FR-012 (`partial`)
  - **Done (2026-08-09):** `IElectronicSignatureHook` + `NullElectronicSignatureHook`; lease preview exposes hook status / request.

- [x] **T013** Remove or rewire unused Blazor template nav (`NavMenu` Counter/Weather) per Constitution I (`unrequested`)
  - **Done (2026-08-09):** `NavMenu.razor` mirrors live ClerkSuite routes.

- [x] **T014** Justify or trim `Syncfusion.Blazor.WordProcessor` until DocumentEditor is required (T6.1) (`unrequested`)
  - **Done (2026-08-09):** Removed WordProcessor package + DocumentEditor script/usings; DocIO/Pdf + SfPdfViewer retained; re-add at T6.1 if needed.

**Re-converge (2026-08-09, post T008–T014):** Zero new CRITICAL/HIGH gaps beyond work already tracked in T3.4–T7.x. No Phase 9 Convergence appended.
