# Research: Wiley Apartment Management v1

**Date**: 2026-08-09 | **Feature**: 001-wiley-apartment-v1

## Decision 1: Application hosting on Synology DS225+

**Decision**: Run ASP.NET Core Blazor **Interactive Server** in Docker via Synology Container
Manager. Default: **single container** (app + SQLite). Alternative: app + PostgreSQL 16 or
MariaDB 11 companion container.

**Rationale**: DS225+ with **6 GB RAM recommended** supports two concurrent Blazor circuits.
Interactive Server minimizes client payload. Browser + Docker beats WinForms for multi-clerk
central updates. No Angular (per requirement).

**Alternatives considered**:

| Option                        | Rejected because                                      |
| ----------------------------- | ----------------------------------------------------- |
| Blazor WebAssembly standalone | Split hosting complexity; heavier client story        |
| WinForms desktop              | Per-PC installs; harder concurrent clerks and updates |
| VM + Windows Server           | RAM/licensing overhead on NAS                         |
| Kubernetes                    | Exceeds minimal-parts requirement                     |

## Decision 2: Structured data store

**Decision**: **SQLite** as v1 **production default** — database file on Docker volume bound to
NAS internal storage (`/data/clerksuite.db` inside container). **PostgreSQL 16 or MariaDB 11**
available **only as optional override** via `docker-compose.postgres.yml` — not the default path.

**Rationale**: Simplest single-container deployment (constitution IV). Two clerks + 16 units
is low write concurrency; Blazor Server serializes through one app process. EF Core works with
both providers via configuration switch.

**Critical constraint**: SQLite file MUST NOT live on SMB/NFS share accessed over network —
only on local Docker volume on NAS disk (avoids locking corruption).

**Alternatives considered**:

| Option                         | When used                                                                   |
| ------------------------------ | --------------------------------------------------------------------------- |
| PostgreSQL / MariaDB companion | **Optional override only** — explicit town IT opt-in; not default           |
| SQLite on SMB share            | **Rejected** — network locking risk                                         |
| Dapper-only (no EF)            | **Rejected** for v1 — EF needed for Identity, migrations, audit interceptor |
| JSON files only                | **Rejected** — no grid/query integrity                                      |

## Decision 3: Document storage

**Decision**: Synology shared folder mounted at **`/volume1/apartments/docs`** into container
as `/docs`. Subpaths: `leases/`, `templates/`, `uploads/`, `appliances/`; metadata in DB.

**Rationale**: Matches town NAS layout; Hyper Backup covers share; clerks use in-app File
Manager, not File Station.

**Alternatives considered**:

| Option                            | Rejected because                             |
| --------------------------------- | -------------------------------------------- |
| Store PDFs only in database bytea | Bloats DB; harder clerk recovery from backup |
| Client-side OneDrive sync         | Violates data-on-NAS principle               |

## Decision 4: Authentication (no role differentiation)

**Decision**: ASP.NET Core Identity with cookie auth. **Login required** so every mutation is
attributed in AuditLog. **No role differentiation** — every authenticated user has full access
to all features. Remove Clerk / ReadOnly / Elevated roles. Seed **1–2 full-access accounts**
on first deploy (passwords chosen at deploy).

**Rationale**: Simplifies v1 for two staff; audit still captures *who* changed data. Role-based
access deferred unless town requests it later.

**Alternatives considered**:

| Option            | Rejected because                                           |
| ----------------- | ---------------------------------------------------------- |
| Clerk + ReadOnly  | **Rejected** — G7 locked: no role differentiation in v1    |
| Synology SSO only | Couples app to NAS login; clerks may not have DSM accounts |
| Azure AD          | Internet dependency; overkill for two clerks               |

## Decision 5: Audit implementation

**Decision**: EF Core `SaveChangesInterceptor` writes `AuditEntry` rows with JSON snapshots of
changed properties; audit table has no UPDATE/DELETE grants for app user.

**Rationale**: Central enforcement — cannot forget to audit a module. Append-only at DB role
level where supported; app layer rejects audit mutations.

## Decision 6: Syncfusion component mapping & UI mandate

**Decision**: **Strict Syncfusion Blazor only** for all UI. Develop/refine using Agentic UI Builder,
`sf-blazor-mcp` (`sf_blazor_assistant`), and official Blazor docs. Non-Syncfusion UI is out of
compliance.

| Surface         | Syncfusion component               |
| --------------- | ---------------------------------- |
| Grids           | SfGrid                             |
| Dashboard       | SfDashboardLayout, SfCard, SfChart |
| Lease templates | SfDocumentEditor                   |
| PDF read        | SfPdfViewer                        |
| File vault      | SfFileManager                      |
| Forms / dialogs | SfDataForm, SfDialog               |

**Rationale**: Constitution V (strict); clerk-quality enterprise UI; MCP-assisted correct APIs.

## Decision 7: Colorado lease templates

**Decision**: Ship default SFDT templates in `templates/leases/` covering standard Colorado
residential fields (parties, premises, term, rent, security deposit, notice addresses);
clerks edit via in-app template manager stored on NAS.

**Rationale**: Template edits without deploy; preview before finalize satisfies FR-005/FR-006.
Legal review once at deploy; clerk maintains wording thereafter.

**Note**: Templates are starting points — town counsel should review before production use.

## Decision 8: Concurrency

**Decision**: Optimistic concurrency via row version tokens on Tenant, Unit, Lease, Payment;
UI shows refresh prompt on conflict.

**Rationale**: Two clerks editing same record is edge case but must not silently lose data
(spec edge cases).

## Decision 9: Fixed 16-unit portfolio (Town of Wiley, CO)

**Decision**: Location is **Town of Wiley, Colorado** (apartment portfolio vicinity per town
maps reference). Seed and enforce maximum 16 units in domain validation; **Unit 1–16 placeholders**
until town supplies real unit numbers, building names, and addresses.

**Rationale**: Town operates exactly 16 unique apartment units; placeholders unblock T1.1 seed
while real list is deferred.

## Decision 10: Payment portal integration (Town PayStar)

**Decision**: Deep-link to **Town of Wiley PayStar portal** — entry via [townofwiley.gov](https://www.townofwiley.gov)
pay-bill flow → `secure.paystar.io`. Configurable `PaymentPortalUrl` env setting; **deep-link only**
(no embedded payment processing in ClerkSuite).

**Rationale**: Out of scope for v1 PCI/complexity; staff get one-click access from tenant/lease view (FR-016).

## Decision 11: Appliance and carpet inventory

**Decision**: First-class `ApplianceAsset` and `CarpetRecord` entities under Unit; maintenance
can link to asset; documents attach to assets.

**Rationale**: FR-1 requires queryable warranty/appliance data and flooring history without
spreadsheet side systems.

## Decision 12: Ledger vs QuickBooks

**Decision**: ClerkSuite maintains operational rent ledger (charges, payments, late fees);
QuickBooks remains town finance source of truth — no sync in v1.

**Rationale**: Spec out-of-scope; avoids double-entry complexity on NAS.

## Decision 13: Data access layer

**Decision**: Entity Framework Core for all CRUD, Identity, migrations, and audit interceptor.
Dapper optional later for heavy reports if profiling shows need.

**Rationale**: Plan specifies EF primary; Dapper only if lighter read paths needed. No Dapper
in v1 unless rent-roll query profiling demands it.

## Decision 14: UTC storage, America/Denver display

**Decision**: Persist all `DateTime` in UTC; convert to `America/Denver` for clerk-facing UI and reports.

**Rationale**: Consistent audit timestamps; Colorado town operates in Mountain Time.

## Decision 15: Soft deletes

**Decision**: Soft delete (`IsDeleted`) for **Tenant** and **Lease**; governed entities never hard-deleted.

**Rationale**: Occupancy and lease history matter for disputes; audit trail references stable IDs.

## Decision 16: Polymorphic Document entity

**Decision**: `Document` uses `EntityType` + `EntityId` (Unit, Tenant, Asset, Lease, MaintenanceRequest); file bytes on NAS; DB metadata only.

**Rationale**: Matches plan item 3; avoids proliferating nullable FK columns.

## Decision 17: Syncfusion licensing & keys (Keychain only)

**Decision**:

- **License tier:** Syncfusion **Community license** (full access for eligible use).
- **Source of truth:** macOS Keychain on dev MacBook — real values **never** in Spec Kit files or git.
- **Runtime (app):** `SYNCFUSION_LICENSE_KEY` via dotnet user-secrets or env; `Program.cs` → `RegisterLicense` from `IConfiguration`.
- **MCP / Agentic (dev only):** prefer `Syncfusion_API_Key_Path` → `~/.config/syncfusion/api.key` (populated once from Keychain); alternates: Keychain bridge script or session env.
- **NAS prod:** `SYNCFUSION_LICENSE_KEY` in container env only; MCP API key not deployed.

**Rationale**: Legal compliance + constitution V; Community tier confirmed; separate license (app) vs API key (MCP); enforceable T0.0 gate.

## Decision 18: Document editor fallback

**Decision**: In-browser DocumentEditor/PdfViewer for normal files; if file exceeds size threshold or editor times out, offer download → external edit → re-upload.

**Rationale**: Mitigates RAM/performance risk on DS225+ for very large documents.

## Decision 19: Syncfusion toolchain (dev environment)

**Decision**: Before any UI code (T0.0), verify on dev MacBook:

1. Syncfusion Blazor NuGet packages (app)
2. `sf-blazor-mcp` / `@syncfusion/blazor-assistant` (Cursor MCP)
3. Agentic UI Builder skill: `apm install syncfusion/blazor-ui-builder -t cursor`
4. Component skills: `npx skills add syncfusion/blazor-ui-components-skills -g`

License via Keychain → user-secrets / env + `RegisterLicense`. MCP API via `Syncfusion_API_Key_Path`
(`~/.config/syncfusion/api.key`) or Keychain bridge — never in repo.

**Rationale**: Constitution V strict mandate; T0.0 gates T0.1; prevents non-compliant AI UI and license watermarks.

## Decision 20: Late fees (settings toggle — default OFF)

**Decision**: Late fees controlled by **`LateFeesEnabled` settings toggle — default OFF** (no late
fee). When enabled, **amount** and **grace days** are configurable; staff can assess late fees
via ledger (T4.2). No automatic assessment until enabled.

**Rationale**: G2 locked; town may not charge late fees initially; rules must be adjustable without code deploy.

## Decision 21: SQLite production default (G4)

**Decision**: **SQLite single-container is the production default.** Postgres/MariaDB compose
override exists for explicit opt-in only — not chosen unless town IT overrides.

**Rationale**: Constitution IV minimal parts; 16 units / 2 staff; G4 locked.

## Decision 22: NAS access — Tailscale + SSH (G6)

**Decision**: Synology DS225+ is reachable via **Tailscale + SSH** for development deploy,
volume setup, and T0.3 document-share verification. Container Manager deploy uses same NAS paths;
Tailscale provides secure remote access when not on town LAN.

**Rationale**: G6 locked; enables MacBook → NAS workflow without exposing DSM broadly.

## Decision 23: Product gaps — resolved / deferred (planning gate 2026-08-09)

| Gap                | Resolution                                                   | Status                          |
| ------------------ | ------------------------------------------------------------ | ------------------------------- |
| G1 Unit list       | Town of Wiley, CO; Unit 1–16 placeholders                    | **Closed** (real list deferred) |
| G2 Late fees       | Toggle default OFF; T4.2                                     | **Closed**                      |
| G3 Payment portal  | PayStar / townofwiley.gov pay-bill; `PaymentPortalUrl`       | **Closed**                      |
| G4 Database        | SQLite default; Postgres override only                       | **Closed**                      |
| G5 Syncfusion tier | Community license; Keychain process                          | **Closed**                      |
| G6 NAS access      | Tailscale + SSH for T0.3 / deploy                            | **Closed**                      |
| G7 Roles           | No differentiation; auth for audit; 1–2 full-access accounts | **Closed**                      |
| G8 Lease templates | Ship blank SFDT; counsel review before go-live               | **Deferred** (T3.2)             |
| G9 Data import     | Manual entry v1                                              | **Deferred**                    |
| G10 App URL        | LAN / Tailscale until reverse proxy                          | **Deferred** (T0.2, T7.1)       |
| G11 Account naming | 1–2 accounts; passwords at deploy                            | **Closed** (merged with G7)     |

## Decision 24: Dev target framework (.NET 9)

**Decision**: Local dev and Docker images target **net9.0** / `aspnet:9.0` (dev MacBook has no
net8 SDK). Original plan cited .NET 8 LTS; net9 is acceptable for v1 unless town IT requires net8 pin.

**Rationale**: Build toolchain availability; Syncfusion 34.x supports net9 Blazor Interactive Server.
