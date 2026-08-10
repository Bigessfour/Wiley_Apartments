# ClerkSuite quick reference

Town of Wiley apartments · **16 residential units + Community Center (CC)** · clerks only

Open: `http://mr-storage:8082` (Tailscale / LAN). Chrome or Edge on Windows 11.

Sign in with your seeded town account. Prefer **Sign out** when leaving a shared PC.

---

## Daily path (happy path)

| Step | Where         | What to do                                                                                                                                                                                                                |
| ---- | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1    | **Units**     | Confirm unit status (Vacant / Occupied / Maintenance / Make-Ready). Open a unit for appliances, flooring, maintenance. Facility row **CC** is the Community Center (not one of the 16 residential slots).                 |
| 2    | **Tenants**   | Add or edit tenant; start/end occupancy on the tenant detail page. Use **Security deposit** card to see required/paid/held and **Record deposit**.                                                                        |
| 3    | **Leases**    | **New lease** wizard → pick unit + tenant → generate fillable PDF → preview / download → upload signed PDF when returned.                                                                                                 |
| 4    | **Payments**  | Filter by tenant/unit → **Record payment** or **Post charge** → balance updates. Check **Open receipt after save** (or grid **Receipt**) → preview PDF → **Print** / **Save PDF**. Use **Generate rent charges** monthly. |
| 5    | **Documents** | NAS folder browse (top) or metadata upload (link to unit/tenant). Receipts also land under tenant **Receipt** category. **View** PDFs in-browser; other types use **Download**.                                           |
| 6    | **Dashboard** | Occupancy gauge, collected-vs-expected + LinearGauge rate + sparkline, unit-status doughnut, 12-month rent trend (zoom/pan), payment HeatMap, lease expirations (30/60), work orders, delinquencies, warranties, schedule reminders, YTD P/L (3D by unit + series). Drag/resize panels persist per browser. Export charts / Reset layout on header.                                |

---

## Menu map

| Menu                 | Use it for                                                                           |
| -------------------- | ------------------------------------------------------------------------------------ |
| Dashboard            | At-a-glance risk + council P/L                                                       |
| Units                | Portfolio of 16 residential + facility chip for CC; assets & flooring on unit detail |
| Tenants              | People records, occupancy, contacts                                                  |
| Leases               | Generate, renew, terminate, PDF preview                                              |
| Payments             | Tenant ledger (not QuickBooks)                                                       |
| Schedule             | Cleaning / vacancy / inspection calendar (drag to reschedule)                        |
| Maintenance          | Work orders across units                                                             |
| **Community Center** | Hub + CC-scoped schedule, payments, maintenance, unit record                         |
| Documents            | Vault on NAS + PDF viewer                                                            |
| Reports              | Rent roll, occupancy, warranty, delinquency, ops costs, portfolio P/L, rent pivot    |
| Audit                | Who changed what (append-only)                                                       |
| Settings             | Light/Dark theme, late fees (default off), PayStar portal link                       |

---

## Community Center (facility)

1. Sidebar → **Community Center** → **CC hub** (or open unit **CC** from Units).
2. Use **CC schedule / payments / maintenance** links — they open the same clerk tools pre-filtered to the facility unit.
3. Do **not** delete unit CC (facility guard). Edit layout notes / sq ft on the unit record as needed.
4. Documents vault is shared NAS-wide; upload under Documents and associate with the CC unit when linking metadata.

---

## Payment portal (PayStar)

Settings → **Payment portal** opens the town pay-bill page (external). ClerkSuite never stores card data — deep-link only.

---

## Payment receipts (print / save)

1. **Payments** → filter tenant/unit → **Record payment** (leave **Open receipt after save** checked), **or** click **Receipt** on an existing payment row.
2. Receipt page shows the town PDF in the viewer. Use header **Print** or **Save PDF** (also printer / download icons on the toolbar).
3. To email a tenant: **Save PDF**, then attach the file in Outlook/mail. ClerkSuite does not send email yet.
4. A copy is saved once under tenant documents (**Receipt**). Regenerating does not upload duplicates.
5. **Audit** shows `PaymentReceipt` / `Generate` for each receipt.

---

## Security deposits

1. **Tenants** → open tenant → **Security deposit** card (required from unit, paid/held from ledger).
2. **Record deposit** posts a deposit payment on the ledger (also available as **Security deposit payment** on Payments).
3. Optionally open the receipt after save the same way as rent payments.

---

## Appearance

Settings → **Appearance** → Light / Dark. Preference is stored in this browser only. Syncfusion controls follow Fluent 2.

---

## Settings — document storage & demo data

| Control                | Use                                                                                                                |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------ |
| **Document root path** | Folder on mr-storage (container usually `/docs` → host `/volume1/apartments/docs`). Save after IT moves the share. |
| **Load demo data**     | Fills Jordan Reyes (24 months) + Community Center renters with PayStar-style payments for training/validation.     |
| **Validate demo**      | Checks tenants, ledger, leases, docs on disk, CC rentals against the database.                                     |

## If something looks wrong

1. Refresh the page (Dashboard has a **Refresh** button).
2. Confirm you are on Tailscale / town LAN and the address uses port **8082**.
3. Ask IT to check NAS container health and Hyper Backup (see `deploy/synology/BACKUP-RESTORE.md`).
4. Check **Audit** for the last successful save of the record you expected.

---

## Clerk review (T7.3)

| Clerk   | Date reviewed | OK? | Notes |
| ------- | ------------- | --- | ----- |
| Clerk A |               |     |       |
| Clerk B |               |     |       |

**Done when:** this one-pager exists and at least one clerk has signed the table above.
