## Surface: F7 `/community-center/reservations/{id}`
**Date:** 2026-08-11
**Reviewer:** builder (agent)
**Environment:** local `http://localhost:5077`
**Build / image / commit:** live + ErrorMessage-hides-UI fix

### 1. Arrive
- [x] Deep link loads Draft → Confirmed → Completed lifecycle
- [x] Subtitle = status + local start–end
- [x] Primary actions in header row (Confirm / money / agreement / complete)

### 2. Format
- [x] PageHeader + SfButton / SfUploader / ClerkPdfViewer / inspection form
- [x] Dark theme OK
- [x] Loading… vs not-found with **All reservations** recovery
- [x] Errors as warning banner **alongside** body (fixed: previously replaced entire page)

### 3. Connected
- [x] Confirm → `SetStatusAsync(Confirmed)` + schedule upsert
- [x] Generate agreement → vault path + in-page PDF viewer + Preview enabled
- [x] Post deposit + fee → guards disable re-post; Record payment unlocks
- [x] Record payment (full $250) → Open receipt `WR-2026-0811-3AE` (`/payments/receipt/{id}`)
- [x] Save PostRental inspection → Mark completed unlocks → Completed
- [x] Renter link → `/community-center/renters/{id}`
- [x] Attach signed PDF + inspection photo UI present (file pick not automated)
- [x] Domain: complete requires PostRental; payment requires charges first

### 4. Usable — job story
> “As clerk, I need agreement, money, and post-rental inspection on one booking so that I can finish the hall rental.”

- [x] Happy path end-to-end on `5c02fcc8-033c-424b-8b26-7edc23906ff3` (~8 clicks)
- [x] Validation/guards plain (disabled buttons + titles + banners)
- [ ] Destructive: Cancel booking has **no confirm dialog** (S2)
- [x] Print/export via agreement viewer + receipt Print/Save PDF

### 5. Bugs
| Sev | Issue                                                                                  | Repro                                 | Fix                                                                               |
| --- | -------------------------------------------------------------------------------------- | ------------------------------------- | --------------------------------------------------------------------------------- |
| S1  | Any `ErrorMessage` hid the whole reservation UI (no recovery)                          | Fail/generate error then stay on page | Banner + keep body; clear on successful Load; not-found header + All reservations |
| S2  | Cancel booking has no confirmation                                                     | Click Cancel                          | Optional SfDialog (defer)                                                         |
| S3  | ClerkPdfViewer sometimes shows native “Choose File” chrome; content still in a11y/text | Agreement/receipt preview             | Known viewer quirk; Print/Download still work                                     |
| S3  | Signed PDF / inspection attach not live-tested                                         | Needs file                            | UI wired; service path from T104/T108                                             |

### 6. Verdict
- [ ] PASS — D2
- [x] PASS WITH NOTES (S1 fixed in session; remaining S2/S3 only)
- [ ] FAIL (open S0/S1)

**Training notes:** Order: Confirm → Generate agreement → Post charges → Record payment → PostRental inspection → Mark completed. Receipt opens from **Open receipt**. Cancel has no undo prompt—use carefully.

**Evidence IDs:** reservation `5c02fcc8-033c-424b-8b26-7edc23906ff3`; payment `3ae36437-4d1a-4bee-945a-b7a60b02d0c3`; receipt `WR-2026-0811-3AE`.
