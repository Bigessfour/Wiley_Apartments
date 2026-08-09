# ClerkSuite — Wiley Apartment Management

**WileyApartments** (solution/repo) · **ClerkSuite** (clerk UI brand)

Internal Town of Wiley system for managing **16 apartment units**, tenants, leases, payments,
maintenance, appliance/carpet inventory, and documents.

**Repository**: [github.com/Bigessfour/Wiley_Apartments](https://github.com/Bigessfour/Wiley_Apartments)

**Status**: Phase 1 **complete** (T1.1–T1.4) — Phase 2 tenants next ([tasks.md](specs/001-wiley-apartment-v1/tasks.md))

## Users

- **Town staff (1–2 accounts)** — full access when authenticated; no role differentiation in v1
- **Tenants** — no login in v1 (PayStar payment portal link only)

## Governance

- [Constitution](.specify/memory/constitution.md) v1.2.0
- [AGENTS.md](AGENTS.md) — Syncfusion MCP UI rules
- [Spec roadmap](specs/README.md)
- [Feature 001 spec](specs/001-wiley-apartment-v1/spec.md) — FR-1 through FR-7

## Spec Kit workflow

| Step                    | Status                                                                                   |
| ----------------------- | ---------------------------------------------------------------------------------------- |
| `/speckit-constitution` | Done v1.2.0 — Syncfusion mandate (strict) + Keychain keys                                |
| `/speckit-specify`      | Done — ClerkSuite FR-1–FR-7                                                              |
| `/speckit-plan`         | Done                                                                                     |
| `/speckit-tasks`        | Done — Phases 0–7                                                                        |
| `/speckit-analyze`      | Done — [analyze-report](specs/001-wiley-apartment-v1/checklists/analyze-report.md)       |
| **Planning gate**       | **Passed** 2026-08-09 — [READINESS.md](specs/001-wiley-apartment-v1/READINESS.md)        |
| `/speckit-implement`    | **Phase 1 done** — T2.1 tenants next ([tasks.md](specs/001-wiley-apartment-v1/tasks.md)) |

## Stack (from plan)

.NET 9 Blazor Interactive Server · Syncfusion · SQLite (default) or Postgres/MariaDB · DS225+ · `/volume1/apartments/docs`

## Out of scope (v1)

Tenant portal (except payment link) · listing syndication · QuickBooks sync · native mobile apps
