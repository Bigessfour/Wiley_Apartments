# ClerkSuite — Wiley Apartment Management

**WileyApartments** (solution/repo) · **ClerkSuite** (clerk UI brand)

Internal Town of Wiley system for managing **16 apartment units**, tenants, leases, payments,
maintenance, appliance/carpet inventory, and documents.

**Repository**: [github.com/Bigessfour/Wiley_Apartments](https://github.com/Bigessfour/Wiley_Apartments)

**Status**: Phase 3 **in progress** (T3.1–T3.3 leases + signed vault upload done) — T3.4 renew/terminate; Phase 3.5 Scheduler next ([tasks.md](specs/001-wiley-apartment-v1/tasks.md))

## Users

- **Town staff (1–2 accounts)** — full access when signed in through the ClerkSuite Cognito session; no apartments password form
- **Tenants** — no login in v1 (PayStar payment portal link only)

## Sign-in

Staff use the Town of Wiley staff pool (`Townofwiley-staff`, `us-east-2_bHk9UcenK`) and the `townofwiley-staff-web` app client. A browser that already has that hosted-UI session is signed in from the token claims. A cold visit shows **Sign in on ClerkSuite** and does not ask for an apartments password. Details: [docs/cognito-sign-in.md](docs/cognito-sign-in.md).

## Governance

- [Constitution](.specify/memory/constitution.md) v1.2.0
- [AGENTS.md](AGENTS.md) — Syncfusion MCP UI rules
- [Spec roadmap](specs/README.md)
- [Feature 001 spec](specs/001-wiley-apartment-v1/spec.md) — FR-1 through FR-7

## Spec Kit workflow

| Step                    | Status                                                                                                                                           |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| `/speckit-constitution` | Done v1.2.0 — Syncfusion mandate (strict) + Keychain keys                                                                                        |
| `/speckit-specify`      | Done — ClerkSuite FR-1–FR-7                                                                                                                      |
| `/speckit-plan`         | Done                                                                                                                                             |
| `/speckit-tasks`        | Done — Phases 0–7                                                                                                                                |
| `/speckit-analyze`      | Done — [analyze-report](specs/001-wiley-apartment-v1/checklists/analyze-report.md)                                                               |
| **Planning gate**       | **Passed** 2026-08-09 — [READINESS.md](specs/001-wiley-apartment-v1/READINESS.md)                                                                |
| `/speckit-implement`    | **T2.3 + T3.1–T3.2 done** — fillable Brookside PDF + SfPdfViewer; Phase 3.5 Scheduler queued ([tasks.md](specs/001-wiley-apartment-v1/tasks.md)) |

## Stack (from plan)

.NET 9 Blazor Interactive Server · Syncfusion · SQLite (default) or Postgres/MariaDB · DS225+ · `/volume1/apartments/docs`

## Out of scope (v1)

Tenant portal (except payment link) · listing syndication · QuickBooks sync · native mobile apps
