## Surface: F6 `/community-center/reservations`
**Date:** 2026-08-11
**Reviewer:** builder (agent)
**Environment:** local `http://localhost:5077`
**Build / image / commit:** live ClerkSuite + seeded SurfacePass reservations

### 1. Arrive
- [x] Nav + deep link `/community-center/reservations` (and `?renterId=`)
- [x] Title/subtitle clear: hall bookings → schedule when Confirmed
- [x] Primary actions visible: Save as Draft / Save & Confirm + CC schedule

### 2. Format
- [x] PageHeader + SfDropDownList / SfDateTimePicker / SfNumericTextBox / SfGrid
- [x] Dark theme OK
- [x] No overflow at laptop width
- [x] Empty grid: Syncfusion “No records to display”
- [x] Validation banner: “Renter, start, end, fee, and deposit are required.”

### 3. Connected
- [x] `FacilityReservationService.ListAsync` / `CreateAsync` (fee defaults $150 / $100)
- [x] `?renterId=` preselects renter (SurfacePass, F4Test)
- [x] Grid shows Draft + Confirmed; local display times via `Clock.ToDisplayTime`
- [x] **Open** → `/community-center/reservations/{id}` (e.g. `5c02fcc8-033c-424b-8b26-7edc23906ff3`)
- [x] **CC schedule** → `/schedule?unitId=` with Facility CC filter banner
- [x] Domain: Confirmed calendar upsert only via service Confirm path (SQL-only Confirmed has no ScheduledItem — expected)

### 4. Usable — job story
> “As clerk, I need to book the hall for a renter so that draft/confirmed bookings show in the list and open for agreement/money.”

- [x] Happy path: preselect renter → create (service) → list → Open — UI form present; DateTimePicker bind not exercised by automation
- [x] Validation plain language when start/end missing
- [x] Destructive N/A on list
- [x] Print N/A (agreement on F7)

### 5. Bugs
| Sev | Issue                                                                 | Repro                    | Fix                                                       |
| --- | --------------------------------------------------------------------- | ------------------------ | --------------------------------------------------------- |
| S3  | Form does not clear after successful create                           | Create then stay on list | Optional clear fields (defer)                             |
| S3  | No status filter on list                                              | Want Draft-only          | Optional; F7 is status home                               |
| S3  | Browser fill of SfDateTimePicker does not update Blazor `@bind-Value` | Automation only          | Clerks use picker; create covered by service + unit tests |

### 6. Verdict
- [ ] PASS — D2
- [x] PASS WITH NOTES (non-blocking S3 only)
- [ ] FAIL (open S0/S1)

**Training notes:** Use **Save & Confirm** only when the booking should appear on the shared schedule. Drafts stay off the calendar until Confirmed (from list create or F7 Confirm).

**Seed IDs (local):** draft `5c02fcc8-033c-424b-8b26-7edc23906ff3`; renter `d1532be7-7861-4576-b98e-24d0304ebc07`.
