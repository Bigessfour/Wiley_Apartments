# AGENTS.md — WileyApartments / ClerkSuite

Rules for AI agents (Cursor, Spec Kit implement) working on this repo.

## Syncfusion UI Mandate (Strict — Constitution Principle V)

**Sole UI stack:** Blazor **Interactive Server** + **Syncfusion Blazor** only.

**Before T0.1 / any `.razor` UI file:** complete **T0.0** toolchain verification.

### T0.0 — Toolchain checklist

| Step                    | Command / config                                                                          |
| ----------------------- | ----------------------------------------------------------------------------------------- |
| Blazor UI Builder skill | `apm install syncfusion/blazor-ui-builder -t cursor` (or current Syncfusion docs)         |
| Component skills        | `npx skills add syncfusion/blazor-ui-components-skills -g`                                |
| Blazor MCP              | `sf-blazor-mcp` in `~/.cursor/mcp.json` → `~/.cursor/scripts/run-sf-blazor-mcp.sh`        |
| License (runtime)       | Keychain → `SYNCFUSION_LICENSE_KEY` via user-secrets; `RegisterLicense` in `Program.cs`   |
| API key (MCP)           | `Syncfusion_API_Key_Path` → `~/.config/syncfusion/api.key` (preferred) or Keychain bridge |

**Done when:** MCP responds; minimal `Sf*` component renders without watermark/key errors.

### UI workflow

1. Call **`sf-blazor-mcp`** → `sf_blazor_assistant` for component/scenario (**mandatory** — not optional).
2. Use [Agentic UI Builder](https://www.syncfusion.com/explore/agentic-ui-builder/) + installed skills for scaffolds (**mandatory**).
3. Implement per [Syncfusion Blazor docs](https://blazor.syncfusion.com/documentation/introduction/).
4. **Do not** add MudBlazor, Radzen, plain HTML data tables, or custom grids.
5. Non-compliant UI → rewrite before task done.

### MCP (Cursor)

| Server          | Tool                  | Use for                         |
| --------------- | --------------------- | ------------------------------- |
| `sf-blazor-mcp` | `sf_blazor_assistant` | Blazor API, patterns, debugging |

Keep `sf-blazor-mcp` active (~4–6 MCP limit). Never ask user to paste keys in chat.

### Secrets (Keychain → env — never in repo)

- **SYNCFUSION_LICENSE_KEY** — app runtime; Keychain → user-secrets / NAS env; `RegisterLicense` in `Program.cs`
- **Syncfusion_API_Key_Path** — preferred MCP path: `~/.config/syncfusion/api.key` (populate once from Keychain)
- **Syncfusion_API_Key** — alternate MCP session env (not committed)
- **Never** in Spec Kit markdown, source, docker-compose (committed), appsettings, or git

Setup steps: [READINESS.md §8](specs/001-wiley-apartment-v1/READINESS.md)

## Stack (summary)

- .NET 8 Blazor Interactive Server + Syncfusion Blazor (sole UI)
- EF Core + SQLite (default) or Postgres/MariaDB
- Synology DS225+ Docker; docs at `/volume1/apartments/docs`
- Identity; AuditLog append-only

## Spec Kit

- Constitution: `.specify/memory/constitution.md` v1.2.0
- Feature: `specs/001-wiley-apartment-v1/`
- **Do not implement** until [READINESS.md](specs/001-wiley-apartment-v1/READINESS.md) gate passed
- Start implement at **T0.0**, then T0.1

## Repository

https://github.com/Bigessfour/Wiley_Apartments
