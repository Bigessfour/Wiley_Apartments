# NAS dual-clerk smoke residual (D2 live)

**When:** After deploy to `mr-storage` / production image  
**Who:** Clerk A + Clerk B (or builder + one clerk)  
**Timebox:** ~15–20 minutes  

Automated CI covers **dev** login + page chrome (`ClerkHappyPathE2ETests`).  
This checklist covers **production NAS** realities (paths, PayStar, printers, dual users).

## Prerequisites

- [ ] App URL live (e.g. `http://mr-storage:8082`)
- [ ] Two clerk accounts from `SeedUsers` (not only `clerk@dev.local`)
- [ ] DocumentRoot mounted and writable
- [ ] Brookside templates present under DocumentRoot/templates (if testing lease generate)

## Smoke steps

| # | Job | Pass criteria |
|---|-----|----------------|
| 1 | Sign in (Clerk A) | Lands on dashboard; name/email in header |
| 2 | Units | Open one unit; see assets or empty inventory without error |
| 3 | Payments | Record $1 test payment (or demo tenant); optional receipt opens PDF |
| 4 | Delete/void test line if needed | Or leave tagged note “smoke test” |
| 5 | Rent roll print | Print dialog / PDF works on clerk workstation |
| 6 | Documents | Upload or open one file from vault |
| 7 | Sign out / Sign in Clerk B | Same smoke #2 or #5 without auth errors |
| 8 | Theme toggle | Light/dark both readable |

## Sign-off

| Role | Name | Date | Pass? |
|------|------|------|-------|
| Clerk A | | | |
| Clerk B | | | |
| IT (optional paths) | | | |

When signed, copy a one-liner into `docs/handover/T7-STATUS.md` or close residual in SPECKIT.
