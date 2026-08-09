# Spec Kit Analyze Report — 001-wiley-apartment-v1

**Date**: 2026-08-09 | **Type**: Cross-artifact consistency (read-only)

**Artifacts reviewed**: `spec.md`, `plan.md`, `tasks.md`, `data-model.md`, `research.md`, `constitution.md`, `quickstart.md`, `contracts/services.md`

---

## Summary

**Overall**: Artifacts are **consistent and implementation-ready** at the planning level. No constitution violations. Minor naming drift and environmental unknowns documented in [READINESS.md](../READINESS.md).

---

## Constitution alignment

| Principle            | Spec                         | Plan                                      | Tasks                  | Verdict |
| -------------------- | ---------------------------- | ----------------------------------------- | ---------------------- | ------- |
| I Clerk-first        | FR-1–FR-7 daily workflows    | Blazor + Syncfusion UX                    | Phases 1–6 clerk tasks | PASS    |
| II NAS data          | NFR, FR-5                    | `/volume1/apartments/docs`, SQLite volume | T0.3, T6.1, T7.2       | PASS    |
| III Audit            | FR-7, FR-025                 | AuditLog interceptor                      | T0.4, T7.4             | PASS    |
| IV Minimal parts     | NFR 2 users                  | 1 container default                       | Phase 0 focus          | PASS    |
| V Syncfusion mandate | Strict Syncfusion-only + MCP | AGENTS.md, plan § Mandate                 | T0.1, T1.4+            | PASS    |
| VI Security          | FR-7                         | Identity, reverse proxy                   | T0.4                   | PASS    |
| VII Colorado         | FR-3, assumptions            | SFDT templates                            | T3.2                   | PASS    |
| VIII Demonstrable    | FR acceptance, SC-*          | quickstart                                | T7.1                   | PASS    |

---

## Coverage matrix (FR → Phase → Task)

| FR                   | Spec section                       | Task phase | Tasks     | Covered |
| -------------------- | ---------------------------------- | ---------- | --------- | ------- |
| FR-1                 | Units, assets, carpet, maintenance | 1          | T1.1–T1.4 | Yes     |
| FR-2                 | Tenants, occupancy                 | 2          | T2.1–T2.3 | Yes     |
| FR-3                 | Leases                             | 3          | T3.1–T3.4 | Yes     |
| FR-4                 | Ledger, portal, reports            | 4          | T4.1–T4.4 | Yes     |
| FR-5                 | Documents                          | 6          | T6.1      | Yes     |
| FR-6                 | Dashboard, reports                 | 6          | T6.2–T6.3 | Yes     |
| FR-7                 | Auth, audit                        | 0          | T0.4      | Yes     |
| Maintenance (FR-1/5) | Spec US1, FR-005                   | 5          | T5.1–T5.2 | Yes     |

---

## Duplications / ambiguities

| ID  | Severity | Finding                                                                           | Recommendation                                         |
| --- | -------- | --------------------------------------------------------------------------------- | ------------------------------------------------------ |
| A1  | Low      | Spec uses `ApplianceAsset`/`DocumentMetadata`; data-model uses `Asset`/`Document` | Use data-model names in code; optional spec edit later |
| A2  | Low      | T0.6 (UTC/Denver) in tasks but not in user's original Phase 0 list                | Keep T0.6 — plan decision 6 requires it                |
| A3  | Medium   | Late-fee rules unspecified                                                        | Resolve G2 in READINESS before T4.2                    |
| A4  | Medium   | Payment portal URL unknown                                                        | Resolve G3 before T4.3 deploy                          |
| A5  | Low      | "Elevated" role in spec FR-024 but optional in plan                               | Default ReadOnly only unless user wants Elevated       |

---

## Underspecified (non-blocking for T0.1)

- 16 unit identifiers (G1)
- Syncfusion license confirmation (G5)
- Production DB choice sign-off SQLite vs Postgres (G4)
- Lease template legal source document (G8)

---

## Task ↔ Spec acceptance mapping

Every FR acceptance checkbox in `spec.md` maps to at least one task **Done when** and quickstart section:

| FR acceptance theme | Tasks      | Quickstart                   |
| ------------------- | ---------- | ---------------------------- |
| 16 units CRUD       | T1.1       | FR-1 section                 |
| Ledger + portal     | T4.1–T4.3  | FR-4 section                 |
| Dashboard < 3s      | T6.2, T6.3 | FR-6 section                 |
| E2E both clerks     | T7.1       | Pre-flight + all FR sections |
| Backup restore      | T7.2       | Backup drill                 |

---

## Verdict

**Planning artifacts: APPROVED for implement gate pending user READINESS sign-off.**

No CRITICAL or Major inconsistencies across spec / plan / tasks. Proceed to T0.1 only after [READINESS.md](../READINESS.md) §4 gaps are answered or explicitly deferred.
