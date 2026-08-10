# Wiley Apartments — Spec Roadmap

**Product**: ClerkSuite | **Repo**: [Bigessfour/Wiley_Apartments](https://github.com/Bigessfour/Wiley_Apartments)

**Phase:** Planning gate **passed** — [implement at T0.0](./001-wiley-apartment-v1/tasks.md)

## Active spec

| Spec                                                       | Scope                               | Status               |
| ---------------------------------------------------------- | ----------------------------------- | -------------------- |
| [001-wiley-apartment-v1](./001-wiley-apartment-v1/spec.md) | ClerkSuite v1 — 16 units, FR-1–FR-7 | **Pilot on NAS** (T7 clerk sign-off remaining) |

## Next version (planned — not started)

| Item | Spec home | Status |
| ---- | --------- | ------ |
| **NV-1 Payment receipt PDF** (print / email after clerk accepts payment) | [plan.md § Next version](./001-wiley-apartment-v1/plan.md) · [tasks.md](./001-wiley-apartment-v1/tasks.md) | Backlog for **v1.1 / 002** |
| DocuSign / e-sign · CC reservations | same | Backlog |

## Task phases (authoritative)

| Phase | Focus                                                      | Tasks     |
| ----- | ---------------------------------------------------------- | --------- |
| 0     | Scaffolding, Docker, auth, audit, NAS, **Syncfusion T0.0**, logging/tests | T0.0–T0.7 **done** |
| 1     | Units + assets + flooring                                  | T1.1–T1.4 |
| 2     | Tenants + occupancy                                        | T2.1–T2.3 |
| 3     | Leases                                                     | T3.1–T3.4 |
| 4     | Payments + ledger                                          | T4.1–T4.4 |
| 5     | Maintenance                                                | T5.1–T5.2 |
| 6     | Documents + dashboard + reports                            | T6.1–T6.3 |
| 7     | Hardening + handover                                       | T7.1–T7.4 |

Full list with **Done when** criteria: [tasks.md](./001-wiley-apartment-v1/tasks.md)

## Artifacts

- [spec.md](./001-wiley-apartment-v1/spec.md) · [plan.md](./001-wiley-apartment-v1/plan.md)
- [data-model.md](./001-wiley-apartment-v1/data-model.md) · [research.md](./001-wiley-apartment-v1/research.md)
- [contracts/](./001-wiley-apartment-v1/contracts/) · [quickstart.md](./001-wiley-apartment-v1/quickstart.md)

## Next command

```
/speckit-implement
```

Start at **T0.0** (Syncfusion toolchain), then T0.1
