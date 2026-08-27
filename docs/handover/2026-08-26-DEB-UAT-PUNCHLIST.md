# Deb UAT punchlist — 2026-08-26

**Reviewer:** Deb (town clerk) — agent-driven chrome-devtools walk of local ClerkSuite
**Environment:** `http://localhost:5077` · `clerk@dev.local`
**Bar:** D2 Daily-ops Ready (no open S0/S1)

**Status after 26 Aug evening fix:** S1-1 and S1-2 **cleared** on local (regenerate lease → Stephen McKitrick / $2,234.00 merged; occupancy 1/16; Generate rent posted unit-roster charge). S2-1 and S2-11 also landed. Re-tick D2 after Deb confirms print + rent collection.

Full surface notes: [surface-logs/2026-08-26-deb-uat.md](./surface-logs/2026-08-26-deb-uat.md).


Did **not** click Settings → Force reseed.

---

## S1 — blocks daily work

| #    | Issue                                                                                                                                                                    | Repro                               | Clerk impact                  |
| ---- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------- | ----------------------------- |
| S1-1 | **Fixed.** Residential lease PDF now merges (Stephen McKitrick, Unit 1, $2,234.00; no `@@` after regenerate). Cause was DOCX regenerate using AcroForm leftover markers. | Leases → Unit 1 draft → Regenerate  | Print a real apartment lease. |
| S1-2 | **Fixed.** Generate PDF copies rent/deposit onto the unit and starts occupancy if vacant. Dashboard 1/16; Generate rent posted $2,234 roster charge.                     | Generate lease PDF on a vacant unit | Collect this month’s rent.    |

---

## S2 — workaround exists

| #     | Issue                                                                                                    | Workaround                                                 |
| ----- | -------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------- |
| S2-1  | CC hub Documents → `/documents?cc=1` (**fixed**). FileManager still lists the whole vault (S2-2).        | Sidebar CC Documents already used `?cc=1`.                 |
| S2-2  | Documents FileManager still shows the whole vault even with `?cc=1`; upload still says “Link to Tenant”. | Filter is metadata-only; clerks must pick CC entity types. |
| S2-3  | Dashboard **Export charts** downloads PNGs with no toast.                                                | Check Downloads folder.                                    |
| S2-4  | Dashboard schedule reminder rows go to `/schedule` without `unitId`.                                     | Filter on the calendar.                                    |
| S2-5  | Occupancy KPI card is not a link.                                                                        | Open Units or Reports → Occupancy.                         |
| S2-6  | Ledger **Include former** mixes residential + CC into one running balance.                               | Filter to a unit, or keep Include former off.              |
| S2-7  | Calendar week view is noisy (current-time line). Steve Entire Facility shows midnight–midnight.          | Use month view / edit times on the reservation.            |
| S2-8  | Occupancy report is grid-only (charts live on dashboard).                                                | Use dashboard doughnut.                                    |
| S2-9  | P/L “By unit” labels overlap / hard to read.                                                             | Use the grid below the chart.                              |
| S2-10 | Settings **Dark** click did not flip `data-theme` in this session.                                       | Stay on Light (default).                                   |
| S2-11 | Generate rent empty result (**fixed**): warning banner + info toast.                                     | Already posted this month, or no occupancy/rent.           |
| S2-12 | Operating costs / warranty charts empty until data exists.                                               | Expected empty; add a cost or asset.                       |

---

## S3 — polish

- Tenant household “Heather Spou” truncated in the grid.
- Units list: 0 beds / $0 rent / layout-notes placeholders on most units.
- Audit Before/After is raw JSON (readable by IT, not Deb).
- Dashboard doughnut legend “Vacant” twice; 3D P/L empty with no occupancy series.
- CC inventory demo is two items; Include zero stayed at 2.

---

## Verified working (do not re-open as bugs)

Login; 21 nav links; CC rental agreement PDF merge; reservation gating (Confirm / Complete / Payment); fail inspection → High WO on Unit CC + audit row; payment form; calendar reminder hours; CC overlap copy; report hub cards; rent pivot 2026; P/L income includes Unit 1 and Unit CC; operating-cost empty-save validation; lease wizard fields; unit detail → New work order (`?unitId=`); CC renters list/detail; Error page copy; inspection reservation deep link.

---

## Out of scope (unchanged)

Live DocuSign, tenant portal/ACH, DocumentEditor, multi-property, public CC booking, clerk-vs-admin RBAC, NAS restore-drill, `Asset.PhotoPaths`.

---

## Suggested next

Remaining S2/S3 only (export toast, reminder `unitId`, dark theme, FileManager CC filter). Deb re-test: print Unit 1 lease + Generate rent. `/code-review` on this slice before commit.
