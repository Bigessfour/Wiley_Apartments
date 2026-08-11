# Surface inventory TEMPLATE

**App:** \<name\> · repo \<org/repo\>  
**Stack:** \<UI kit / hosting\>  
**Repo playbook:** `docs/<APP>-SURFACE-COMPLETION.md` (create if missing)  
**Default target:** D2 Daily-ops Ready  

Copy this file to `references/<app>-surfaces.md` (or `docs/…`) and fill from
routes / nav. Delete unused section rows; keep section letters stable once
progress logging starts.

## Out of D2 scope (do not reopen as blockers)

- \<deferred portal / e-sign / multi-tenant SaaS / …\>  
- \<anything signed out of this pass\>  

## Inventory

Status values: **Unchecked** | **PASS D2** | **PASS WITH NOTES** | **FAIL**

### A. Access & shell
| ID | Route | Role job story | Status |
|----|-------|----------------|--------|
| A1 | `/login` (or equiv.) | As operator, I reach the app without auth loops | Unchecked |
| A2 | Shell (nav, header, theme, logout) | As operator, I can reach every in-scope surface | Unchecked |
| A3 | `/error` (or equiv.) | Rare; confirm no dead crash page | Unchecked |

### B. Master data
| ID | Route | Role job story | Status |
|----|-------|----------------|--------|
| B1 | | As \<role\>, I need to ____ so that ____ | Unchecked |
| B2 | | | Unchecked |

### C. Workflows / lifecycle
| ID | Route | Role job story | Status |
|----|-------|----------------|--------|
| C1 | | As \<role\>, I complete create → edit → close | Unchecked |

### D. Money
| ID | Route | Role job story | Status |
|----|-------|----------------|--------|
| D1 | | As \<role\>, I record / review money without wrong totals | Unchecked |

### E. Ops queues
| ID | Route | Role job story | Status |
|----|-------|----------------|--------|
| E1 | | As \<role\>, I work the daily queue end-to-end | Unchecked |

### F. Command / dashboard
| ID | Route | Role job story | Status |
|----|-------|----------------|--------|
| F1 | `/` or hub | As \<role\>, KPIs deep-link to real filtered lists | Unchecked |

### G. Reports
| ID | Route | Role job story | Status |
|----|-------|----------------|--------|
| G1 | | As \<role\>, I print/export what I need for the job | Unchecked |

### H. Admin
| ID | Route | Role job story | Status |
|----|-------|----------------|--------|
| H1 | `/settings` | As admin, I configure safely (no prod-dangerous seeds) | Unchecked |

## Recommended finish order

Follow the **operator’s daily path**, not curiosity:

1. Access & shell (A*)  
2. Master data list → detail (B*)  
3. Lifecycle wizards (C*)  
4. Money (D*)  
5. Ops queues (E*)  
6. Dashboard / command — after data exists (F*)  
7. Reports operators actually use (G*)  
8. Documents / audit if present  
9. Admin last (H*)  

Example ID chain (replace with yours):  
`A1 → A2 → B1 → B2 → C1 → D1 → E1 → F1 → G1 → H1`

## Severity (blocks D2?)

| Sev | Meaning | Blocks D2? |
|-----|---------|------------|
| **S0** | Data loss, wrong money, security | Yes |
| **S1** | Operator job blocked | Yes |
| **S2** | Workaround exists | No (notes) |
| **S3** | Polish only | No |

## D2 pass rule

A surface may be marked **PASS D2** or **PASS WITH NOTES** only when it has
**no open S0 or S1**. Do not fake PASS without a connectivity / job-story
review. If live preview is unavailable, complete code connectivity review and
list residual **manual operator** checks explicitly.

## Progress

Maintain a progress log in the repo playbook (e.g. `docs/…-SURFACE-COMPLETION.md`)
so finishing survives chat sessions. Use `references/session-template.md` for
each surface.

## Example inventory

For a filled ClerkSuite / Wiley Apartments example, see
`references/clerk-suite-surfaces.md` (example only — replace for other apps).
