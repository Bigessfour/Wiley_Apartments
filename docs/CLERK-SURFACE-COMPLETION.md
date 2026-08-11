> **Agent skill:** `.grok/skills/project-completion/` — open `SKILL.md` when finishing surfaces.
> References: `done-levels.md`, `session-template.md`, `clerk-suite-surfaces.md`.

# Clerk surface completion workflow

**Purpose:** Finish ClerkSuite by walking **every clerk-touch surface** the same way a professional PM product would UAT before go-live.  
**Audience:** Builder + clerk (or dual-clerk).  
**Definition of Done (this pass):** not “code exists,” but **a clerk can complete the job on that page without dead ends, wrong data, or Syncfusion chrome that fights them.**

---

## 1. Three levels of “Done” (pick which bar you mean)

| Level | Meaning | ClerkSuite today (honest) |
|-------|---------|---------------------------|
| **D1 — Spec Done** | FR/tasks accepted; deployable; dual-clerk signed T7 | **Yes** (SPECKIT-DONE 2026-08-10) |
| **D2 — Daily ops Ready** | Every nav surface works end-to-end with live links, empty states, no console errors, data matches clerk mental model | **Mostly** — needs this surface pass |
| **D3 — Market-polished** | Matches AppFolio/Buildium-class UX density, guided workflows, bulk ops, mobile-first, audit-grade E2E | **No** — not the goal for 16-unit town portfolio |

**This workflow targets D2.** D1 is already signed. D3 is optional product ambition, not required for town ops.

---

## 2. How we compare to professional market products

| Dimension | Typical market (AppFolio, Buildium, Yardi Breeze, DoorLoop) | ClerkSuite (Wiley) | Verdict |
|-----------|--------------------------------------------------------------|--------------------|---------|
| **Scope** | Full PM: online pay, tenant portal, listings, accounting GL, bulk comms | Town clerk ops for **~16 units + CC**: occupancy, leases, ledger, WO, schedule, vault, reports | Intentional smaller scope |
| **Stack / UI kit** | Proprietary design systems | **Syncfusion Blazor** end-to-end | Aligned with *your* constitution |
| **Auth** | SaaS identity | Cookie/local auth for NAS | Fine for intranet |
| **Data model** | Multi-property, multi-entity | Single portfolio, facility unit (CC) | Right-sized |
| **Workflow depth** | Wizards, bulk post rent, online ACH | Lease wizard, rent post, receipts, deposits | Competitive for size |
| **Reporting** | Packaged + custom | Rent roll, delinq, warranty, P/L, pivot, occupancy | Good for council/clerk |
| **Documents** | Cloud DMS | NAS FileManager + metadata | Fits DS225+ reality |
| **E-sign** | Built-in or DocuSign | **Null hook** (post-v1) | Gap vs market; accepted deferral |
| **Tenant self-serve** | Portal + pay | PayStar link only | Gap vs market; OK if clerks post payments |
| **Mobile** | First-class apps | Responsive Blazor | Weaker than market apps |
| **QA automation** | Broad E2E | Mostly **auth-gate** E2E + solid unit tests | Below market engineering bar |
| **Polish / empty states / deep links** | Consistent product language | Strong on dashboard; **variable** page-to-page | This pass closes the gap |

**Bottom line:** For a **single small town portfolio on self-hosted NAS**, ClerkSuite is closer to a **custom vertical tool** than a SaaS PM suite. Against AppFolio on features, we lose on portal/pay/e-sign/mobile. Against “can two clerks run Wiley apartments day-to-day without Excel,” we can win **if D2 surface pass is clean**.

---

## 3. Surface inventory (nav order = clerk mental model)

Checkboxes are for the completion pass. Status starts **Unchecked** until a full session below is recorded.

### A. Access & shell
- [x] **A1** Login `/Account/Login`
- [x] **A2** Shell: sidebar, header, theme, PayStar link, toast host, logout
- [x] **A3** Error `/Error` (rare path)

### B. Command center
- [x] **B1** Dashboard `/`
- [x] **B2** Reports hub `/reports` + each report (see H)

### C. Portfolio master data
- [x] **C1** Units list `/units` (+ `?status=`)
- [x] **C2** Unit detail `/units/{id}` (assets, flooring, occupancy, ops costs, WO snippet)
- [x] **C3** Tenants list `/tenants`
- [x] **C4** Tenant detail `/tenants/{id}` (household, vehicles, pets, deposits, history)

### D. Lease lifecycle
- [x] **D1** Leases list `/leases`
- [x] **D2** New lease wizard `/leases/new`
- [x] **D3** Lease preview/lifecycle `/leases/{id}` (PDF, amend/renew/terminate, signed vault, e-sign hook messaging)

### E. Money
- [x] **E1** Ledger / payments `/payments` (+ `?unitId=`)
- [x] **E2** Payment receipt `/payments/receipt/{entryId}`
- [x] **E3** Late fee settings path via Settings (batch)

### F. Operations
- [x] **F1** Schedule `/schedule` (+ unit filter)
- [x] **F2** Maintenance `/maintenance` (+ `?unitId=`)
- [x] **F3** Community Center hub `/community-center` + deep links

### G. Documents & compliance
- [x] **G1** Documents `/documents` (FileManager + metadata)
- [x] **G2** Audit `/audit`

### H. Reports (each must export/print or copy usable for council/clerk)
- [x] **H1** Rent roll
- [x] **H2** Delinquency
- [x] **H3** Occupancy
- [x] **H4** Warranty
- [x] **H5** Maintenance costs
- [x] **H6** Operating costs
- [x] **H7** Portfolio P/L
- [x] **H8** Rent pivot

### I. Admin
- [x] **I1** Settings `/settings` (paths, late fees, demo seed — **seed only non-prod**)

### Explicitly out of D2 (post-v1 / market gaps)
- DocuSign real provider (null hook OK if clerks upload signed PDF)
- CC reservation PDF
- Tenant portal / ACH inside app
- Multi-property

---

## 4. Session template (use for EVERY surface)

Copy this block into `docs/handover/surface-logs/YYYY-MM-DD-<id>.md` or a running PR comment.

```markdown
## Surface: <ID> <route>
**Date:**  
**Reviewer:** builder / clerk A / clerk B  
**Environment:** local | NAS mr-storage:8082  
**Build/image:**  

### 1. Arrive
- [ ] Nav or deep link reaches page (no 404 / auth loop)
- [ ] Title + subtitle make the job obvious
- [ ] Primary action visible without scrolling on laptop

### 2. Format (Syncfusion + layout)
- [ ] Uses Syncfusion controls consistent with neighbors (Grid/Schedule/etc.)
- [ ] Fluent theme contrast OK (light + dark if used)
- [ ] No horizontal overflow at 1280px and ~390px
- [ ] Loading and empty states are real (not blank white)
- [ ] Errors show toast or banner with recovery (retry / fix field)

### 3. Connected (data & links)
- [ ] Reads from the correct service (not stale mock)
- [ ] Create / edit / soft-delete persists and reloads
- [ ] Every row/action that *should* open detail **does** (id in URL)
- [ ] Cross-links work: unit ↔ tenant ↔ lease ↔ payment ↔ WO ↔ docs
- [ ] Facility/CC rules correct where relevant
- [ ] Deposits vs rent distinguished where money shows

### 4. Usable (clerk job story)
Write the job story, then do it:
> “As clerk, I need to ____ so that ____.”

- [ ] Happy path completes in ≤ N clicks (note count)
- [ ] Validation messages are plain language
- [ ] Destructive actions confirm
- [ ] Print/export if the job needs a paper trail

### 5. Bugs found
| Sev | Issue | Repro | Fix PR |

### 6. Verdict
- [ ] **PASS — D2** (ready for daily work)
- [ ] **PASS WITH NOTES** (non-blocking)
- [ ] **FAIL** (blocking; do not check inventory box)

**Notes for clerk training:**
```

### Severity rubric
| Sev | Meaning |
|-----|---------|
| **S0** | Data loss, wrong money, security |
| **S1** | Job blocked (can't post rent, open lease, etc.) |
| **S2** | Workaround exists (extra clicks, wrong label) |
| **S3** | Polish only |

**D2 pass rule:** no open S0/S1 on that surface.

---

## 5. Recommended order (finish-friendly)

Do **not** jump by curiosity. Order by **clerk daily path**:

1. **A1–A2** Login + shell  
2. **C1–C2** Units (portfolio truth)  
3. **C3–C4** Tenants  
4. **D1–D3** Leases (including one full wizard → PDF → status)  
5. **E1–E2** Payments + receipt  
6. **F2** Maintenance  
7. **F1** Schedule  
8. **B1** Dashboard (verify links land on data you just created)  
9. **H*** Reports that council/clerk actually use (rent roll, delinq, P/L, warranty)  
10. **G1–G2** Documents + audit  
11. **F3** Community Center  
12. **I1** Settings (careful with demo seed on NAS)

**Cadence:** one surface per focused session (25–45 min). Log → fix S0/S1 same day → re-verify → check box.  
Avoid implementing new features during this pass unless required to clear S0/S1.

---

## 6. Cross-cutting “connected” checks (run once after core path)

| Check | How |
|-------|-----|
| Dashboard → list → detail round-trip | Click each home KPI/list row; confirm filter or entity |
| Post rent → dashboard collection / delinq update | Post payment; refresh home + delinquency report |
| Lease expire windows | Fixture or demo lease ends in ≤30 and 31–60; home lists match |
| WO open → home count | Create WO; dashboard count increments |
| Soft-delete tenant/unit rules | Soft-deleted hidden from default lists; no FK crash |
| CC excluded from occupancy | Occupancy counts ignore facility |
| Deposit never in rent totals | Payment marked deposit; collection KPIs unchanged |

---

## 7. Syncfusion “aligned” bar (per surface)

Not “uses a Syncfusion control,” but:

1. **Right control for the job** (Grid for tabular edit, Schedule for calendar, FileManager for vault, Charts for viz).  
2. **Documented props** for that package version (see `docs/dashboard-syncfusion-review.md` pattern).  
3. **Theme** Fluent2 / Fluent2Dark consistent.  
4. **No raw Bootstrap-only islands** where the rest of the app is Syncfusion (login may stay simple).  
5. **Toolbar/action patterns** match adjacent pages (PageHeader + SfButton outline primary actions).

---

## 8. Progress log

| Date | Surface | Verdict | PR / notes |
|------|---------|---------|------------|
| 2026-08-11 | A1–A2, C1–C4, D1, I1 guard | PASS WITH NOTES | See `docs/handover/surface-logs/2026-08-11-d2-pass-batch1.md`; unit cap + WO link + prod demo wipe fixes |
| 2026-08-11 | D2–D3 leases | PASS WITH NOTES | Wizard excludes facility; preview lifecycle OK |
| 2026-08-11 | E1–A3 batch 2 | PASS WITH NOTES | `surface-logs/2026-08-11-d2-pass-batch2.md` |
| | NAS clerk smoke residual | | sign-in, payment, print, vault |

When all inventory boxes are PASS D2 or PASS WITH NOTES (no S0/S1), mark:

**CLERK SURFACE COMPLETION — D2 READY** in SPECKIT or handover.

---

## 9. How to run this with an agent (Grok Build)

1. Pick next unchecked surface from §5.  
2. Agent opens page code + service + tests; builder or agent exercises live NAS/local.  
3. Fill session template; file S0/S1 fixes immediately.  
4. Re-verify; check inventory; commit log row.  
5. Stop for the day after 1–3 surfaces — finishing favors **closed loops**, not breadth.

**Next surface to start:** **A1 Login** then **A2 Shell**, then **C1 Units list**.


---

## Status (2026-08-11)

**SURFACE COMPLETION — D2 CODE READY** (connectivity pass complete).  
All inventory surfaces logged **PASS WITH NOTES**. Remaining residual: live dual-clerk smoke on NAS (`mr-storage`) — sign-in, post payment + receipt, print rent roll/P/L, open document vault file.
