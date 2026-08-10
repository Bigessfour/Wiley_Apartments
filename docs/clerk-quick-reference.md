# ClerkSuite quick reference

Town of Wiley apartments · 16 units · clerks only

Open: `http://mr-storage:8082` (Tailscale / LAN). Chrome or Edge on Windows 11.

Sign in with your seeded town account. Prefer **Sign out** when leaving a shared PC.

---

## Daily path (happy path)

| Step | Where         | What to do                                                                                                                                      |
| ---- | ------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| 1    | **Units**     | Confirm unit status (Vacant / Occupied / Maintenance / Make-Ready). Open a unit for appliances, flooring, maintenance.                          |
| 2    | **Tenants**   | Add or edit tenant; start/end occupancy on the tenant detail page.                                                                              |
| 3    | **Leases**    | **New lease** wizard → pick unit + tenant → generate fillable PDF → preview / download → upload signed PDF when returned.                       |
| 4    | **Payments**  | Filter by tenant/unit → **Post charge** or **Post payment** → balance updates in the grid. Use **Generate rent charges** for the monthly cycle. |
| 5    | **Documents** | NAS folder browse (top) or metadata upload (link to unit/tenant). **View** PDFs in-browser; other types use **Download**.                       |
| 6    | **Dashboard** | Occupancy cards, lease expirations (30/60), work orders, delinquencies, warranties, YTD P/L charts.                                             |

---

## Menu map

| Menu        | Use it for                                                                        |
| ----------- | --------------------------------------------------------------------------------- |
| Dashboard   | At-a-glance risk + council P/L                                                    |
| Units       | Portfolio of 16; assets & flooring on unit detail                                 |
| Tenants     | People records, occupancy, contacts                                               |
| Leases      | Generate, renew, terminate, PDF preview                                           |
| Payments    | Tenant ledger (not QuickBooks)                                                    |
| Schedule    | Cleaning / vacancy / inspection calendar (drag to reschedule)                     |
| Maintenance | Work orders across units                                                          |
| Documents   | Vault on NAS + PDF viewer                                                         |
| Reports     | Rent roll, occupancy, warranty, delinquency, ops/maintenance costs, portfolio P/L |
| Audit       | Who changed what (append-only)                                                    |
| Settings    | Light/Dark theme, late fees (default off), PayStar portal link                    |

---

## Payment portal (PayStar)

Settings → **Payment portal** opens the town pay-bill page (external). ClerkSuite never stores card data — deep-link only.

---

## Appearance

Settings → **Appearance** → Light / Dark. Preference is stored in this browser only. Syncfusion controls follow Fluent 2.

---

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
