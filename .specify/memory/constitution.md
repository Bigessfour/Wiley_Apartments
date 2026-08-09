<!--
  Sync Impact Report
  Version change: 1.1.0 → 1.2.0
  Modified principles: V — mandatory Agentic UI Builder / MCP / skills; Keychain-only keys
  Added sections: Secure key process (Keychain → env, never in git)
  Follow-up: READINESS § Developer machine setup; T0.0 verification
-->

# Wiley Apartment Management System Constitution

## Core Principles

### I. Clerk-First Reliability
The two town clerks MUST perform every daily task without developer intervention:
unit lookup, tenant update, lease generation, payment recording, and document retrieval.
If a workflow requires a developer, a ticket, or a manual file edit outside the app, it is
not done. UI flows MUST be obvious, forgiving, and recoverable from common mistakes.

### II. Data Lives on the NAS
All structured data and documents MUST reside on the Synology DS225+. No critical data MAY
be stored only on client machines. Application servers, caches, and clients are views into
NAS-backed storage — not the system of record. Backup and restore procedures MUST treat the
NAS as authoritative.

### III. Auditability
Every create, update, and delete of tenant, lease, payment, maintenance, or document MUST
be logged with user, timestamp, and before/after values. Audit logs MUST be append-only,
queryable by clerks for dispute resolution, and durable on the NAS. Silent mutations are
forbidden.

### IV. Minimal Moving Parts
Prefer simple, robust solutions that run reliably on a 2–6 GB RAM NAS with two concurrent
Windows 11 users. Avoid optional microservices, heavy containers, or dependencies that
require constant babysitting. Complexity MUST earn its place against clerk reliability and
NAS resource limits.

### V. Syncfusion Mandate (Strict)
**Sole UI stack:** Blazor **Interactive Server** + **Syncfusion Blazor** only.

All UI **must** use **Syncfusion Blazor** components per official documentation and recommended
patterns. Pages, layouts, dashboards, grids, document viewers, and forms **must** be generated
or refined using Syncfusion [Agentic UI Builder](https://www.syncfusion.com/explore/agentic-ui-builder/),
Blazor MCP (`sf-blazor-mcp` / `sf_blazor_assistant`), Blazor UI Builder skill, and component
skills — **mandatory** for implementation agents, not optional. Non-Syncfusion UI libraries for
data grids, charts, dialogs, file pickers, or document editing are **forbidden** when Syncfusion
provides the capability. Generic AI UI that ignores documented Syncfusion APIs is **out of compliance**
and must be rewritten before merge. Visual patterns MUST stay consistent so clerks learn once.

**Task T0.0** (Syncfusion toolchain verification) MUST pass before any UI code (T0.1+).

**License & API keys — Keychain only; never in Spec Kit files or git:**

- Keys remain in **macOS Keychain** (source of truth on the dev MacBook).
- **Runtime license (app):** inject via .NET User Secrets or environment variable
  `SYNCFUSION_LICENSE_KEY`; `Program.cs` reads from `IConfiguration` and calls
  `SyncfusionLicenseProvider.RegisterLicense` — **never hardcoded**.
- **MCP / Agentic tools (API key):** prefer `Syncfusion_API_Key_Path` pointing to a local file
  **outside the repo** (e.g. `~/.config/syncfusion/api.key`), populated once from Keychain; or
  `Syncfusion_API_Key` in Cursor MCP / shell env that is **not committed**.
- Keys MUST NOT appear in source, Spec Kit markdown, docker-compose committed files, appsettings,
  or git. Document setup **steps** only — never the values.

### VI. Security by Default
Local network first; **Tailscale** for secure remote NAS access when needed. HTTPS via reverse
proxy when exposed outside the LAN. **Authentication required** for audit attribution; v1 has
**no role differentiation** — every authenticated user has full access. Seed 1–2 staff accounts.
Least privilege for file shares at the NAS layer. Secrets MUST NOT live in the repo or on clerk workstations.

### VII. Colorado-Aware Leasing
Lease templates and notices MUST support Colorado residential rental requirements, or be
easily editable by clerks to do so. Generated documents MUST be reviewable before issuance.
Legal language changes MUST not require code deploys when a template edit suffices.

### VIII. Done Means Demonstrable
A feature is not complete until a clerk can perform the workflow on the real NAS from a
Windows 11 machine and acceptance criteria pass. Narrative "done" without device/NAS proof
is insufficient. Spec Kit tasks MUST tie to demonstrable clerk outcomes.

## Tech Stack Constraints

- **Target users:** One to two town staff on Windows 11 (full access when authenticated)
- **Hosting:** Synology DS225+ on town LAN; **Tailscale + SSH** for remote dev/deploy
- **UI:** Blazor Interactive Server with **Syncfusion Blazor only** (strict — see Principle V)
- **UI tooling (mandatory):** Syncfusion Agentic UI Builder + Blazor UI Builder skill + component
  skills + `sf-blazor-mcp` — verified at **T0.0** before UI implementation
- **Data:** NAS-resident **SQLite** (default, single container); PostgreSQL/MariaDB optional override only
- **Auth:** ASP.NET Core Identity — login for audit; no roles in v1
- **Audit:** First-class module; not bolted on after the fact
- **Forbidden by default:** Public marketing site, portfolio accounting, AI leasing triage,
  native mobile apps (responsive web is sufficient for v1)

Stack details (database, reverse proxy, deployment layout) are decided in feature plans and
MUST comply with principles II and IV.

## Non-Goals (v1)

- Public-facing marketing or listings site
- Complex multi-property portfolio accounting
- Full AI automation of leasing or maintenance triage
- Mobile native apps (responsive web is sufficient)

Items above MAY be reconsidered only via constitution amendment and a new major spec.

## Spec Kit Application

Spec Kit replaces "prompt and pray" with durable intent — not mandatory ceremony.

**Use Spec Kit when** the work changes clerk workflow, NAS data layout, audit behavior,
Syncfusion surfaces, or deployment on the DS225+.

**Skip Spec Kit when** the change is a typo, copy tweak, one-line bugfix, or obvious local
refactor that does not affect clerk outcomes.

**Default loop:** `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` →
`/speckit-implement`. Optional skills (`clarify`, `checklist`, `analyze`, `converge`) are
tools, never gates.

**North star:** Both clerks run daily apartment operations on the NAS from Windows 11 —
lookup, update, lease, pay, retrieve — with full audit trail and no developer babysitting.

## Governance

1. Constitution principles supersede ad-hoc agent improvisation when they conflict.
2. Amendments: update this file, bump version (MAJOR = principle removal/redefinition;
   MINOR = new principle/section; PATCH = clarification), note ratification in commit message.
3. Before merging mid/large features: confirm NAS authority, audit coverage, clerk UX, and
   Syncfusion consistency.
4. Prefer evidence (clerk walkthrough on NAS, acceptance scenarios) over narrative "done."

**Version**: 1.2.0 | **Ratified**: 2026-08-09 | **Last Amended**: 2026-08-09
