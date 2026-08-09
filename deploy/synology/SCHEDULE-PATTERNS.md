# Operations calendar patterns (T3.5.3)

ClerkSuite schedule items live in SQLite (`ScheduledItems`); UI at `/schedule` (Syncfusion SfSchedule).

## Vacancy → turnover clean

**Action:** On `/schedule`, use **Suggest turnover cleans**.

**Behavior:**

- Selects units with status **Vacant**
- Skips units that already have an **open** (not completed) **Cleaning** item
- Creates a **Cleaning** item titled `Turnover clean — unit {N}` for tomorrow 09:00–11:00 America/Denver
- Sets a 1-day reminder offset; notes mark the item as a vacancy turnover suggestion

Clerks can drag/edit/delete suggestions like any other calendar item. Recurring RRULE automation is out of scope for v1 — use the button after each move-out.

## Related

- Categories: Cleaning, Vacancy, Inspection, Other
- Dashboard reminder surfacing: **T6.2**
