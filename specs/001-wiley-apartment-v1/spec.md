# Feature Specification: ClerkSuite — Wiley Apartment Management v1

**Feature Branch**: `001-wiley-apartment-v1`

**Created**: 2026-08-09

**Status**: Planning gate passed — implement at [T0.0](./tasks.md)

**Product name**: **WileyApartments** (repo/solution) · **ClerkSuite** (clerk-facing UI brand)

**Input**: Full-suite internal tool for Town of Wiley clerks to manage **16 unique apartment
units**, tenants, leases, payments, maintenance, appliance/carpet inventory, and documents.
Hosted on Synology DS225+; accessed via browser from two Windows 11 workstations.

**Repository**: [github.com/Bigessfour/Wiley_Apartments](https://github.com/Bigessfour/Wiley_Apartments)

## Vision

A clerk-first internal system where the two town clerks perform every daily task — unit lookup,
tenant update, lease generation, payment recording, document retrieval, maintenance logging, and
dashboard review — without developer intervention. All structured data and documents live on the
NAS. Colorado-aware leasing. Syncfusion-polished UI.

## Users

| User                      | Access                                         | Notes                                         |
| ------------------------- | ---------------------------------------------- | --------------------------------------------- |
| Town staff (1–2 accounts) | Full access to all features when authenticated | Primary users — no role differentiation in v1 |
| Tenants                   | No login in v1                                 | Payment portal deep-link only (PayStar)       |

Authentication is **required** so AuditLog attributes every mutation to a user. All authenticated
users have identical full access — no Clerk / ReadOnly / Elevated roles in v1.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Unit and Asset Management (Priority: P1) — FR-1

A clerk manages all 16 units with unique attributes, tracks appliances and carpet/flooring,
and views maintenance history at unit and asset level.

**Why this priority**: Fixed 16-unit portfolio is the physical foundation for all other records.

**Independent Test**: Create/edit all unit fields; add appliance with warranty dates; add carpet
record; log maintenance on unit and on one appliance; dashboard reflects status change.

**Acceptance Scenarios**:

1. **Given** the 16-unit portfolio, **When** a clerk edits square footage, beds/baths, layout
   notes, and status (Occupied/Vacant/Maintenance/Make-Ready), **Then** changes persist and
   appear on unit detail and dashboard immediately.
2. **Given** a unit, **When** a clerk adds an appliance (make, model, serial, install date,
   warranty start/end, condition), **Then** it is queryable on the unit and searchable by serial.
3. **Given** a unit, **When** a clerk records carpet type, install date, condition, and
   replacement history, **Then** flooring data is visible on unit detail.
4. **Given** maintenance on a unit or specific asset, **When** viewed, **Then** history
   appears on both unit detail and asset detail views.

---

### User Story 2 - Tenant and Occupancy Management (Priority: P1) — FR-2

A clerk creates and maintains tenant records with household members, links occupancy to units,
and retains history when tenants move.

**Why this priority**: Tenant data drives leases, payments, and communications.

**Independent Test**: CRUD tenant with household member; start/end occupancy; soft-delete;
attach screening document; search by name.

**Acceptance Scenarios**:

1. **Given** a new tenant, **When** clerk creates record with contact, emergency contact,
   vehicles, pets, and notes, **Then** record is searchable and editable.
2. **Given** a tenant and vacant unit, **When** clerk starts occupancy, **Then** unit status
   updates and occupancy history records start date.
3. **Given** a moving tenant, **When** clerk ends occupancy, **Then** history is retained and
   unit can be set Make-Ready or Vacant.
4. **Given** an inactive tenant, **When** clerk soft-deletes, **Then** record is hidden from
   default search but history and audit remain.

---

### User Story 3 - Lease Lifecycle (Priority: P2) — FR-3

A clerk generates leases from templates, tracks key dates, and manages renew/amend/terminate
workflows with signed document storage.

**Why this priority**: Leases are legally significant and tie unit to tenant financially.

**Independent Test**: Generate lease PDF from template; track expiring lease on dashboard;
upload signed copy; renew updates dates and status.

**Acceptance Scenarios**:

1. **Given** occupied unit with tenant data, **When** clerk generates lease from template,
   **Then** unit and tenant fields auto-populate and PDF/DOCX is produced for review.
2. **Given** active leases, **When** clerk views dashboard, **Then** leases expiring within
   60 and 30 days are listed with links to detail.
3. **Given** a signed lease scan, **When** uploaded, **Then** it links to the lease record in
   the document vault.
4. **Given** a renewing lease, **When** clerk runs renew/amend/terminate workflow, **Then**
   status and history update correctly (e-signature integration deferred; export-ready in v1).

---

### User Story 4 - Payments and Ledger (Priority: P2) — FR-4

A clerk maintains tenant ledgers with charges, payments, late fees, and links to the town
online payment portal.

**Why this priority**: Daily rent collection and delinquency tracking are core clerk work.

**Independent Test**: Post charge and payment; balance updates; rent roll exports; portal link
opens from tenant view; delinquency list accurate.

**Acceptance Scenarios**:

1. **Given** a tenant account, **When** clerk views ledger, **Then** all charges and payments
   show with running balance.
2. **Given** outstanding balance, **When** clerk records cash/check payment, **Then** balance
   updates immediately and audit logs the entry.
3. **Given** tenant/lease view, **When** clerk clicks payment portal link, **Then** town
   external card portal opens (no tenant login in ClerkSuite).
4. **Given** late-fee rules configured, **When** rent is past due, **Then** late fees can be
   applied and tracked on the ledger.
5. **Given** 16 units, **When** clerk generates rent roll or delinquency list, **Then** report
   reflects current occupancy and balances (QuickBooks remains town finance source of truth).

---

### User Story 5 - Document Vault (Priority: P2) — FR-5

A clerk uploads, categorizes, and views leases, warranties, manuals, inspection photos, and
notices per unit and tenant.

**Why this priority**: Eliminates manual NAS folder browsing for clerks.

**Independent Test**: Upload PDF; open in-browser via Syncfusion viewer; file on Synology share;
File Manager navigation works.

**Acceptance Scenarios**:

1. **Given** a unit or tenant, **When** clerk uploads a categorized document, **Then** it
   retrieves by browse or search and stores on NAS shared folder.
2. **Given** a PDF or common Office file, **When** clerk opens it, **Then** in-browser viewer
   or editor displays without leaving the app (where format supports).
3. **Given** appliance with manual/receipt, **When** attached, **Then** document links from
   asset record.

---

### User Story 6 - Dashboard and Reporting (Priority: P2) — FR-6

A clerk opens the home dashboard for occupancy, expiring leases, open maintenance,
delinquencies, warranty expirations, and portfolio P/L visuals; runs basic reports
including city-council-ready monthly/yearly net income.

**Why this priority**: Proactive clerk work — expirations and delinquency before phone calls;
council review needs clear apartment-level and period P/L without QuickBooks digs.

**Independent Test**: Dashboard loads under 3s on LAN with 16 units seeded; each widget links
to detail; rent roll prints/exports; P/L chart shows per-unit and period net income when
ledger + ops costs exist.

**Acceptance Scenarios**:

1. **Given** real data for 16 units, **When** clerk opens dashboard on LAN, **Then** page
   loads in under 3 seconds with accurate occupancy overview.
2. **Given** dashboard widgets, **When** clerk clicks an indicator (e.g., delinquent account),
   **Then** navigates to the relevant detail view.
3. **Given** reporting menu, **When** clerk runs rent roll, **Then** at least one printable or
   exportable report is available.
4. **Given** posted rent income and unit operating costs, **When** clerk or council reviewer
   opens the P/L dashboard/report, **Then** Syncfusion charts show **P/L per apartment** and
   **monthly / yearly net income** (portfolio), printable/exportable for council packets.

---

### User Story 7 - Auth, Audit, and Access (Priority: P1) — FR-7

Authorized users sign in with role-based access; all significant mutations are audited;
system reachable from both Windows 11 browsers on the local network.

**Why this priority**: Constitution MUST — security and audit are non-negotiable.

**Independent Test**: Unauthorized access blocked; mutation appears in audit log; HTTPS optional
via Synology reverse proxy.

**Acceptance Scenarios**:

1. **Given** valid clerk credentials, **When** sign-in succeeds, **Then** full CRUD is
   available per role.
2. **Given** read-only Mayor/IT account, **When** browsing, **Then** view works; mutations
   are denied.
3. **Given** any significant create/update/delete, **When** audit is queried, **Then** user,
   timestamp, and before/after values are recorded.
4. **Given** LAN clients, **When** clerks browse to app URL, **Then** system is reachable from
   both Windows 11 workstations.

---

### Edge Cases

- Concurrent edits on same tenant or unit: optimistic concurrency with clerk-friendly conflict
  message.
- NAS share unavailable: clear error; no false "saved" state.
- Unit count fixed at 16: system prevents creating unit 17 without admin override (or warns).
- Payment portal link misconfigured: visible error with IT contact note, not silent failure.
- Warranty expiration with no replacement date: dashboard still flags expiring warranty.
- Soft-deleted tenant with open balance: remains visible on delinquency until resolved.

## Requirements _(mandatory)_

### Functional Requirements

**FR-1 Unit Management**

- **FR-001**: System MUST support full CRUD for exactly 16 configurable units.
- **FR-002**: Each unit MUST store square footage, bedrooms/baths, layout notes, and status
  (Occupied, Vacant, Maintenance, Make-Ready).
- **FR-003**: System MUST track nested appliance inventory per unit (make, model, serial,
  install date, warranty start/end, condition, photos, manuals/receipts).
- **FR-004**: System MUST track carpet/flooring (install date, type/material, condition,
  replacement history).
- **FR-005**: System MUST link maintenance history to units and individual assets.

**FR-2 Tenant Management**

- **FR-006**: System MUST support full CRUD for tenants and household members with soft-delete.
- **FR-007**: System MUST link tenant(s) to units with occupancy start/end and retained history.
- **FR-008**: System MUST store contact info, emergency contacts, vehicles, pets, notes, and
  screening documents.

**FR-3 Lease Management**

- **FR-009**: System MUST generate leases from templates auto-populated with unit and tenant
  data and support custom clauses.
- **FR-010**: System MUST manage lease lifecycle with **soft-delete** where history matters.
- **FR-011**: System MUST store signed leases and related paperwork in the document vault.
- **FR-012**: System MUST be e-signature ready (export PDF; integration hook for later).

**FR-4 Payment Management**

- **FR-013**: System MUST maintain tenant ledger with charges, payments, and running balance
  per unit and tenant.
- **FR-014**: System MUST record payments (cash, check, online reference).
- **FR-015**: System MUST generate rent charges/invoices and apply late-fee rules.
- **FR-013a**: System MUST track landlord unit operating costs (Utility, Repair, Replace,
  CommonUpkeep) separately from the tenant ledger so ops expenses never alter tenant balances.
- **FR-016**: System MUST deep-link to town external online payment portal (cards).
- **FR-017**: System MUST provide outstanding balance views, rent roll, and delinquency list
  for 16 units.

**FR-5 Document Management**

- **FR-018**: System MUST provide central vault for leases, warranties, manuals, inspection
  photos, and notices.
- **FR-019**: System MUST organize documents per unit and per tenant with File Manager navigation.
- **FR-020**: System MUST support in-browser viewing/editing via Syncfusion Document Editor and
  PDF Viewer where practical.
- **FR-021**: System MUST store files on Synology shared folders with app-enforced permissions.

**FR-6 Dashboard & Reporting**

- **FR-022**: Dashboard MUST show occupancy, lease expirations (60/30 day), open maintenance,
  delinquent accounts, warranty expirations, and Syncfusion chart widgets for **P/L per unit**
  plus portfolio **monthly/yearly net income** (for city council review).
- **FR-023**: System MUST provide reports: rent roll, occupancy, maintenance cost by unit,
  unit operating costs by category, asset warranty status, and **exportable/printable P/L**
  (per apartment and monthly/yearly portfolio net) suitable for council packets.
  Income from tenant ledger payments/charges; expense from `UnitOperatingCost` (not mixed into
  tenant balances). QuickBooks remains town finance source of truth — ClerkSuite P/L is
  operational review, not audited GL.

**FR-7 Auth, Audit & Access**

- **FR-024**: System MUST authenticate users with role-based access (Clerk, ReadOnly, optional
  Elevated).
- **FR-025**: System MUST log all significant mutations with user, timestamp, before/after.
- **FR-026**: System MUST run on local network with optional HTTPS via Synology reverse proxy.

### Key Entities

- **Unit** (16 max): attributes, status, appliances, carpet, maintenance, documents
- **ApplianceAsset**: nested under unit; warranty, condition, linked docs and maintenance
- **CarpetRecord**: flooring history under unit
- **Tenant**: contact, household, vehicles, pets; soft-delete flag
- **HouseholdMember**: linked to tenant
- **OccupancyHistory**: tenant-unit spans with start/end
- **Lease**: lifecycle, key dates, deposit, status, linked signed doc
- **LedgerEntry** (Charge | Payment): amounts, dates, methods, late-fee flag
- **UnitOperatingCost**: landlord ops expense per unit (or building-wide CommonUpkeep); categories Utility/Repair/Replace/CommonUpkeep
- **LateFeeRule**: configuration for overdue rent
- **MaintenanceRecord**: unit and optional asset link, cost, status
- **ScheduledItem**: calendar item for cleaning/vacancy/inspection/other with optional unit/tenant/lease links
- **DocumentMetadata**: NAS path, category, unit/tenant/asset links
- **AuditEntry**: append-only change log (table: `AuditLog`)
- **User**: Identity with role

## FR Acceptance Criteria (Definition of Done)

Checkboxes below are the **project-level done signals** for each FR group. They mirror the
authoritative spec workup and map to tasks Phases 1–7 and [quickstart.md](./quickstart.md).

### FR-1 Unit Management

- [ ] Clerk can create/edit/view all 16 units with unique attributes.
- [ ] Appliance record captures make/model/serial/install/warranty and is queryable by unit.
- [ ] Carpet install data is stored and visible on unit detail.
- [ ] Maintenance history appears on both unit and asset views.
- [ ] Status changes are reflected immediately on dashboard.

### FR-2 Tenant Management

- [ ] Create, edit, soft-delete, and search tenants works.
- [ ] Occupancy can be started/ended and history is retained.
- [ ] Documents can be attached to a tenant record.

### FR-3 Lease Management

- [ ] Generate a lease PDF/DOCX from template using current unit + tenant data.
- [ ] Key dates are tracked and surface on dashboard (expiring within 60/30 days).
- [ ] Signed lease can be uploaded and linked to the lease record.
- [ ] Amend/renew/terminate workflows update status and history correctly.

### FR-4 Payment Management

- [ ] Ledger shows all charges and payments with running balance.
- [ ] Clerk can record a payment and it updates the balance immediately.
- [ ] Link to external payment portal is present and functional from tenant/lease view.
- [ ] Rent roll and delinquency list can be generated for the 16 units.

### FR-5 Document Management

- [ ] Documents can be uploaded, categorized, and retrieved by unit or tenant.
- [ ] PDF and common Office formats open in-browser via Syncfusion viewers.
- [ ] Files are stored on Synology shared folders with proper permissions.

### FR-6 Dashboard & Reporting

- [ ] Dashboard loads in under 3 s on local network with real data for 16 units.
- [ ] All key indicators are accurate and clickable to detail views.
- [ ] At least one printable/exportable rent-roll report exists.
- [ ] P/L per apartment and monthly/yearly portfolio net income charts are available for council review.

### FR-7 Auth, Audit & Access

- [ ] Only authorized users can access the system.
- [ ] Every significant mutation is logged and viewable by admin.
- [ ] System is reachable from both Windows 11 clients via browser.

**Spec done signal**: All FR groups above have measurable criteria; no open TBD on core workflows.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Clerk completes unit lookup or tenant search in under 30 seconds.
- **SC-002**: Dashboard loads in under 3 seconds on LAN with all 16 units populated.
- **SC-003**: 100% of significant mutations produce audit entries viewable by authorized users.
- **SC-004**: Clerk generates lease PDF and records payment without IT assistance (acceptance
  walkthrough pass).
- **SC-005**: All documents and DB recoverable from NAS backup/snapshot drill alone.
- **SC-006**: Both clerks use system concurrently without data loss in acceptance testing.
- **SC-007**: Rent roll report matches manual spot-check for all 16 units.

## Non-Functional Requirements

- Hosted exclusively on Synology DS225+ (Docker preferred).
- Reliable with 2 concurrent users on 2–6 GB RAM NAS.
- Responsive grids, forms, and document open for daily clerical work.
- Data and documents backed up via Synology Hyper Backup / snapshots.
- Syncfusion Blazor used for **all** major UI surfaces (strict mandate — no alternate component libraries).
- UI implemented per official Syncfusion docs; **mandatory** Agentic UI Builder + MCP + skills (T0.0).
- License/API keys from Keychain only via secure env; never in Spec Kit files or repo (see Plan § UI & Syncfusion Mandate; READINESS §8).

## Assumptions

- Portfolio is fixed at 16 unique town apartment units.
- QuickBooks remains source of truth for town-wide finances; ClerkSuite is operational ledger
  for apartment rent operations.
- Colorado lease templates reviewed by town counsel once; clerks maintain wording thereafter.
- External payment portal URL provided by town IT; ClerkSuite links out only.
- E-signature is export-ready in v1; DocuSign/similar integration is post-v1.
- Initial data import from spreadsheets acceptable for bootstrap.
- **Payment receipts (print/email PDF)** deferred to next version (v1.1) — see plan § Next version.

## Out of Scope (v1)

- Tenant self-service portal (beyond payment portal link).
- Automated listing syndication or applicant screening integration.
- Full double-entry accounting / QuickBooks sync.
- Public marketing/listings site.
- AI leasing or maintenance triage.
- Native mobile apps (responsive web sufficient).
- **Clerk-generated payment receipt PDF** (print or email to tenant after accepting payment) — planned for **v1.1** (not v1).
