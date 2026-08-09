# Quickstart: ClerkSuite v1 Acceptance

**Users**: Two clerks on Windows 11 | **Host**: Synology DS225+ | **Units**: 16

## Pre-flight

1. Containers `wiley-apartments-app` and `wiley-apartments-db` running.
2. NAS share `apartments/docs` mounted at `/volume1/apartments/docs`; Hyper Backup configured.
3. Clerk accounts seeded; `PaymentPortalUrl` set in env.
4. Browse `http://<nas>:8080` from both clerk PCs.

## FR-1 Unit Management

- [ ] Create/edit/view all 16 units with unique sq ft, beds/baths, layout, status
- [ ] Add appliance: make/model/serial/install/warranty — queryable on unit
- [ ] Carpet install data visible on unit detail
- [ ] Maintenance on unit and on one appliance appears in both views
- [ ] Status change reflects on dashboard within one refresh

## FR-2 Tenant Management

- [ ] Create, edit, soft-delete, search tenants
- [ ] Start/end occupancy; history retained
- [ ] Attach screening document to tenant

## FR-3 Lease Management

- [ ] Generate lease PDF from template with unit + tenant data
- [ ] Dashboard shows leases expiring 60/30 days
- [ ] Upload signed lease linked to record
- [ ] Renew/amend/terminate updates status and history

## FR-4 Payment Management

- [ ] Ledger shows charges/payments with running balance
- [ ] Record payment updates balance immediately
- [ ] Payment portal link opens town external site
- [ ] Rent roll and delinquency list for 16 units

## FR-5 Document Management

- [ ] Upload, categorize, retrieve by unit/tenant
- [ ] PDF opens in Syncfusion PdfViewer in-browser
- [ ] File exists on Synology share (not PC-only)

## FR-6 Dashboard & Reporting

- [ ] Dashboard loads < 3 s on LAN with 16 units
- [ ] Widgets accurate and clickable to detail
- [ ] Rent roll printable/exportable

## FR-7 Auth, Audit & Access

- [ ] Unauthorized access blocked
- [ ] Significant mutations in audit log with before/after
- [ ] Both Windows 11 clients reach app via browser

## Backup drill

- [ ] Restore DB volume + document share from snapshot; re-verify FR-2 and FR-5 samples

## Sign-off

| Clerk    | Date | Pass |
| -------- | ---- | ---- |
| Clerk A  |      |      |
| Clerk B  |      |      |
| IT/Mayor |      |      |
