# Clerk acceptance smoke (T7.1)

Run on **each** Windows 11 clerk PC against `http://mr-storage:8082`.

1. Sign in → Dashboard loads widgets.
2. Units → edit a residential unit status → Open unit → note assets tab.
3. Community Center hub → CC schedule opens filtered.
4. Tenants → create/search → occupancy start (if vacant unit available).
5. Leases → wizard generate PDF → preview.
6. Payments → post charge + payment → balance moves.
7. Maintenance → new work order on a unit.
8. Documents → browse vault; open a PDF if present.
9. Audit → latest mutations visible.
10. Sign out.

Record pass/fail in `specs/001-wiley-apartment-v1/quickstart.md` Sign-off table.
