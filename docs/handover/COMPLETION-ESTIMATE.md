# Completion estimate — ClerkSuite after D2 surface pass

**As of:** 2026-08-11  
**Code baseline:** `master` after D2 surface batches 1–2 + optional closeout  
**Done bar used:** D2 Daily-ops Ready (not AppFolio/D3)

---

## Status summary

| Track | Status | Remaining |
|-------|--------|-----------|
| Spec Kit D1 (FR / T7 product) | **Done** (signed 2026-08-10) | T7.2 backup drill unknown (IT) |
| D2 code surface pass | **Done** (all inventory PASS WITH NOTES) | Live NAS clerk smoke |
| Auth’d E2E happy-path | **In this PR** | Green CI |
| NAS dual-clerk smoke | **Checklist only** | 15–20 min human |
| Post-v1 product (DocuSign, portal, CC PDF) | **Deferred** | Separate epics |

---

## Effort remaining (honest)

| Item | Owner | Estimate | Blocks “go to work”? |
|------|-------|----------|----------------------|
| NAS clerk smoke (`NAS-CLERK-SMOKE.md`) | Clerks + optional IT | **0.5 day** (one session) | Soft — recommended before trusting money workflows on live data |
| T7.2 restore drill sign-off | IT | **0.5–1 day** when convenient | No (already non-blocking) |
| Auth E2E maintenance if flaky | Dev | **0–2 hours** if CI flakes | No if unit suite green |
| DocuSign provider (`IElectronicSignatureHook`) | Dev + vendor | **3–5 days** | No — upload signed PDF works |
| In-app ACH / tenant portal | Product | **2–4+ weeks** | No — PayStar link |
| CC reservation PDF (NV-3) | Dev | **1–2 days** | No |
| D3 market polish (mobile app, bulk ops, uniform density) | Product | **ongoing / not scoped** | No |

### Roll-up

| If you mean… | Estimate to call it finished |
|--------------|------------------------------|
| **Code D2 complete** | **0 days** — already merged |
| **Ops-ready on NAS** (smoke signed) | **+0.5 day** calendar |
| **Ops + IT backup drill** | **+0.5–1 day** when IT available |
| **+ e-sign provider** | **+3–5 eng days** |
| **+ tenant portal / ACH** | **weeks** — new product track |

**Recommendation:** Treat project as **complete for Wiley daily clerk work** after the **0.5-day NAS smoke**. Park portal/DocuSign as backlog epics, not open “finish” work.

---

## Confidence

| Area | Confidence |
|------|------------|
| Domain + Syncfusion surfaces | High (surface pass + unit tests + CI) |
| Money correctness (deposits vs rent) | High (tests + ledger services) |
| Dual-clerk concurrent NAS | Medium until smoke signed |
| DocumentRoot / templates on prod | Medium until smoke step 6 |

---

## Optional backlog (do not reopen D2)

1. DocuSign / Adobe Sign hook  
2. Tenant portal / online pay inside ClerkSuite  
3. CC reservation PDF  
4. Authenticated Playwright expand: post payment + assert receipt number  
5. Screenshot refresh under `docs/handover/screenshots/`

---

## Sign-off template

```
ClerkSuite completion estimate reviewed:
- Code D2: YES
- NAS smoke: YES / NO (date ____)
- Remaining epics accepted as post-v1: YES
Owner: ________  Date: ________
```
