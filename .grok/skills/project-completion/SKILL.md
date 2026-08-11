---
name: project-completion
description: >
  Finish multi-surface apps by walking every user-touch surface to Daily-ops
  Ready (D2) — not more features. Use when the user wants to finish, close out,
  UAT, go page-by-page, check clerk/ops usability, find dead links, verify
  "done", compare to market polish, or stop planning and ship for real work.
  Covers Done levels (D1 Spec / D2 Daily-ops / D3 Market), surface inventory,
  session template, severity, finish-friendly order, Syncfusion/component
  alignment, and anti-scope-creep rules. Triggers on "finish", "completion",
  "closeout", "page by page", "surface", "UAT", "ready for production",
  "daily ops", "clerk pass", "is it done", "dead links", "usability pass",
  "go live", "walk through", "project completion".
metadata:
  short-description: "Finish apps surface-by-surface to Daily-ops Ready (D2)"
---

# Project completion (surface pass)

**Goal:** Close a project so a real operator can work—not invent more scope.

This skill is for **finishers’ block**: planning and implementation are strong;
“is it actually ready?” is vague. Replace vibes with a **surface inventory**,
**session template**, and **D2 pass rule**.

**Read on demand (don’t dump all into context):**
- `references/done-levels.md` — D1 / D2 / D3 and honest market comparison
- `references/session-template.md` — copy-paste log for every surface
- `references/surface-inventory.TEMPLATE.md` — blank inventory for any app
- `references/clerk-suite-surfaces.md` — **example** inventory (ClerkSuite /
  Wiley); replace with an app-specific file for other products

For UI polish tokens use **`design-ui`**. For games use **`building-games`**.
This skill owns **completion workflow**, not visual design systems.

---

## 0. When to open this skill

Open when the user says any of:
- finish / close out / UAT / go page by page
- “is it done?” / ready for clerks / production / daily work
- walk every screen / dead links / connected data
- compare to professional products / market readiness

**Do not** open this skill to justify a greenfield rebuild or a new feature
track. If D1 Spec is already signed, default target is **D2**, not D3.

---

## 1. Define Done before touching pages

Three levels—always name which bar you’re shooting for:

| Level | Meaning | Rule of thumb |
|-------|---------|----------------|
| **D1 — Spec Done** | FRs/tasks accepted; deployable; acceptance signed | Spec kit / checklist complete |
| **D2 — Daily-ops Ready** | Every operator surface: formatted, connected, usable; no S0/S1 bugs | **Default finish bar** |
| **D3 — Market-polished** | SaaS-class density, portal, bulk ops, mobile apps, deep E2E | Optional ambition |

**D2 pass rule:** no open **S0** or **S1** on that surface (see §4).

If the user only says “done,” assume **D2** unless they name market parity.

Details and market honesty: `references/done-levels.md`.

---

## 2. Build the surface inventory first

1. List every **user-reachable route** (`@page`, router paths, nav links).
2. Group by operator mental model (access → master data → workflows → money →
   ops → reports → admin)—not by folder name alone.
3. Mark each surface: Unchecked | PASS D2 | PASS WITH NOTES | FAIL.
4. Explicitly list **out of scope** for this pass (deferred e-sign, multi-tenant
   SaaS, etc.) so they don’t re-open as “almost done.”

For **ClerkSuite / Wiley**, start from `references/clerk-suite-surfaces.md`
(example inventory — keep it in sync with nav if that app changes).

For **other apps**, copy `references/surface-inventory.TEMPLATE.md` →
`references/<app>-surfaces.md` and fill from routes/nav (same section
structure and status values). Do not treat the ClerkSuite file as the only
inventory path.

**Persist the inventory** in the repo (`docs/…-COMPLETION.md` or
`docs/handover/surface-logs/`) so finishing survives chat sessions.

---

## 3. One surface, one closed loop

**Cadence:** 1 surface per focused session (or 1–3 max). Never “browse the app.”

**Order (finish-friendly):** follow the **operator’s daily path**, not curiosity.

Typical ops-app order:
1. Login + shell  
2. Core entities (list → detail)  
3. Lifecycle wizards (create → complete)  
4. Money / ledger  
5. Work queues (maintenance, tickets)  
6. Calendar / schedule  
7. Dashboard (prove deep links to data you just created)  
8. Reports operators actually print  
9. Documents / audit  
10. Settings last (dangerous seeds)

**Anti-patterns:**
- New features during the pass unless required to clear S0/S1  
- “While we’re here” refactors  
- D3 polish before D2 connectivity  
- Checking a box without running the job story

---

## 4. Session template (mandatory)

For **every** surface, run the six blocks. Full markdown:
`references/session-template.md`.

### Quick checklist

1. **Arrive** — route works; title states the job; primary action visible  
2. **Format** — right component kit controls; theme; no overflow; loading/empty/error  
3. **Connected** — correct service; persist + reload; IDs in URLs; cross-links; domain rules (facility, deposits ≠ rent, soft-delete)  
4. **Usable** — written job story; happy path; plain validation; confirm destructive  
5. **Bugs** — log with severity  
6. **Verdict** — PASS D2 | PASS WITH NOTES | FAIL  

### Severity

| Sev | Meaning | Blocks D2? |
|-----|---------|------------|
| **S0** | Data loss, wrong money, security | Yes |
| **S1** | Operator job blocked | Yes |
| **S2** | Workaround exists | No (notes) |
| **S3** | Polish only | No |

**Same-day rule:** fix S0/S1 before the next surface. Re-verify, then tick inventory.

---

## 5. “Connected” means graph integrity

A page that renders is not done. Verify the **graph**:

- List → detail (`/{id}` or equivalent)  
- Detail → related entity (unit ↔ tenant ↔ lease ↔ payment ↔ work order ↔ docs)  
- Dashboard / KPI → filtered list or entity  
- Create → appears in list **and** downstream aggregates  
- Soft-delete → hidden from defaults, no FK crash  
- Domain exclusions still hold (e.g. facility units out of occupancy)

Run a short **cross-cutting** round after the core path (see template § cross-checks).

---

## 6. Component-kit alignment (e.g. Syncfusion)

“Aligned” ≠ “imports a control.” Per surface:

1. **Right control for the job** (grid vs schedule vs file manager vs chart)  
2. **Documented props** for the installed major version  
3. **Theme consistency** with the rest of the app  
4. **Shared chrome** (page header, toolbar buttons, empty states)  
5. Prefer a short **API review note** for dense viz pages (pattern:
   ClerkSuite `docs/dashboard-syncfusion-review.md`)

Do not rewrite a working surface to a different kit mid-pass.

---

## 7. Agent execution protocol

When the user says **continue**, **start surface X**, or **finish the app**:

1. Open this skill + the repo’s completion doc / inventory.  
2. Pick the **next unchecked** surface in the recommended order (or the ID they named).  
3. Read page + injected services + related routes (deep links).  
4. Exercise or reason the job story; note S0–S3.  
5. **Implement only S0/S1 fixes** (and trivial S2 if one-liner).  
6. Re-check that surface; update inventory + progress log.  
7. Stop or advance **one** surface—report verdict in product language.

If live preview / NAS isn’t available, still complete **code connectivity**
review and mark residual **manual clerk** checks explicitly—do not fake PASS D2.

---

## 8. When the whole pass is complete

All inventory rows are **PASS D2** or **PASS WITH NOTES** (no open S0/S1), then:

1. Write a one-line handover: **SURFACE COMPLETION — D2 READY** + date.  
2. List deferred D3 / post-v1 items (do not silently expand scope).  
3. Optional: refresh screenshots and clerk quick-reference.

Do **not** reopen D1 Spec Kit unless FRs changed.

---

## 9. Finish checklist (agent)

- [ ] Done level named (default D2)  
- [ ] Surface inventory exists and matches nav/routes  
- [ ] Out-of-scope list written  
- [ ] Current surface ran full session template  
- [ ] S0/S1 fixed or surface marked FAIL (not silently passed)  
- [ ] Progress log updated  
- [ ] No feature creep beyond blockers  
- [ ] User told next surface ID in one line  

---

## 10. Pairing with other skills

| Need | Skill |
|------|--------|
| Visual polish of a surface that already PASSes D2 | `design-ui` |
| Game / canvas | `building-games` (+ `design-ui` for chrome) |
| Auth wiring in app-builder template | `auth` |
| This workflow | **`project-completion`** (you are here) |
