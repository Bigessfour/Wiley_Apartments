# ClerkSuite (Wiley Apartments) surface inventory

> **EXAMPLE inventory** for this skill package. For other apps, copy
> `surface-inventory.TEMPLATE.md` → `<app>-surfaces.md` and fill from routes/nav.
> Do not treat this file as required outside ClerkSuite / Wiley Apartments.

**App:** ClerkSuite · repo `Bigessfour/Wiley_Apartments`
**Stack:** Blazor Server + Syncfusion Blazor 34.x · self-hosted NAS
**Repo playbook:** `docs/CLERK-SURFACE-COMPLETION.md`
**Default target:** D2 Daily-ops Ready

Spec Kit D1 was signed (T7). This inventory is the **D2 finish track**.

## Out of D2 scope (do not reopen as blockers)

- Live DocuSign (null `IElectronicSignatureHook` — upload signed PDF)
- In-app tenant portal / ACH (PayStar link OK)
- Multi-property SaaS

## Inventory (nav order)

### A. Access & shell
| ID  | Route                                                  | Notes |
| --- | ------------------------------------------------------ | ----- |
| A1  | `/Account/Login`                                       |       |
| A2  | Shell (sidebar, header, theme, PayStar, toast, logout) |       |
| A3  | `/Error`                                               | Rare  |

### B. Command
| ID  | Route          | Notes                                 |
| --- | -------------- | ------------------------------------- |
| B1  | `/` Dashboard  | After data path: prove KPI deep links |
| B2  | `/reports` hub | Then H*                               |

### C. Master data
| ID  | Route           | Notes                                  |
| --- | --------------- | -------------------------------------- |
| C1  | `/units`        | `?status=` filter                      |
| C2  | `/units/{id}`   | Assets, flooring, occupancy, ops costs |
| C3  | `/tenants`      |                                        |
| C4  | `/tenants/{id}` | Household, pets, deposits              |

### D. Leases
| ID  | Route          | Notes                        |
| --- | -------------- | ---------------------------- |
| D1  | `/leases`      |                              |
| D2  | `/leases/new`  | Full wizard job story        |
| D3  | `/leases/{id}` | PDF, lifecycle, signed vault |

### E. Money
| ID  | Route                         | Notes      |
| --- | ----------------------------- | ---------- |
| E1  | `/payments`                   | `?unitId=` |
| E2  | `/payments/receipt/{entryId}` |            |

### F. Operations
| ID  | Route                                 | Notes                                                      |
| --- | ------------------------------------- | ---------------------------------------------------------- |
| F1  | `/schedule`                           | Shared calendar; `?unitId=` for CC filter                  |
| F2  | `/maintenance`                        | `?unitId=`; Completer column; optional CC reservation link |
| F3  | `/community-center`                   | Hub + deep links                                           |
| F4  | `/community-center/renters`           | FacilityRenter CRUD (not Tenants)                          |
| F5  | `/community-center/renters/{id}`      | Reservations for renter                                    |
| F6  | `/community-center/reservations`      | Book / confirm                                             |
| F7  | `/community-center/reservations/{id}` | Agreement preview/signed, money, inspections               |
| F8  | `/community-center/inspections`       | Pre/Post rental list                                       |
| F9  | `/community-center/inventory`         | Equipment qty/condition; include-zero filter               |

### G. Documents & compliance
| ID  | Route        | Notes                                    |
| --- | ------------ | ---------------------------------------- |
| G1  | `/documents` | FileManager + metadata (CC entity types) |
| G2  | `/audit`     |                                          |

### H. Reports
| ID  | Route                        |
| --- | ---------------------------- |
| H1  | `/reports/rent-roll`         |
| H2  | `/reports/delinquency`       |
| H3  | `/reports/occupancy`         |
| H4  | `/reports/warranty`          |
| H5  | `/reports/maintenance-costs` |
| H6  | `/reports/operating-costs`   |
| H7  | `/reports/profit-loss`       |
| H8  | `/reports/rent-pivot`        |

### I. Admin
| ID  | Route       | Notes                          |
| --- | ----------- | ------------------------------ |
| I1  | `/settings` | No demo seed on production NAS |

## Recommended session order

A1 → A2 → C1 → C2 → C3 → C4 → D1 → D2 → D3 → E1 → E2 → F2 → F1 → **B1** → H1/H2/H7/H4 (priority reports) → remaining H* → G1 → G2 → **F3 → F4 → F5 → F6 → F7 → F8 → F9** → I1 → A3 if needed

## Domain connection rules (ClerkSuite-specific)

- Residential occupancy excludes `Unit.IsFacility` (Community Center)
- Deposit ledger flags must not inflate rent collection KPIs
- Soft-deleted tenants/leases/WOs stay out of default lists
- Dashboard WO rows should deep-link `maintenance?unitId=`
- Lease expirations: ≤30 and 31–60 day buckets both surface on home

## Syncfusion notes

- Prefer patterns already green in CI (see `docs/dashboard-syncfusion-review.md` for home)
- Linear gauge bar pointer: `PointerValue`
- HeatMap cell JSON: `IsJsonData` + mappings
- Chart3D: `RotationAngle` / `TiltAngle` / `LabelRotationAngle`
- Sparkline namespace: `Syncfusion.Blazor.Sparkline`

## Progress

Maintain the table in `docs/CLERK-SURFACE-COMPLETION.md` § Progress log.
