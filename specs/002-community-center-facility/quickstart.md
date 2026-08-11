# Quickstart: Community Center Facility (002)

**Audience**: Implementers and clerks validating Development builds
**App**: ClerkSuite — `./scripts/run-local.sh` (Development only on Mac)

## Prerequisites

- 001 residential stack builds and runs.
- Facility unit seeded (`Number=CC`, `IsFacility`).
- Syncfusion license via user-secrets / Keychain (never commit keys).
- Spec Kit feature active: `export SPECIFY_FEATURE=002-community-center-facility`

## Clerk happy path (after implementation)

1. **Community Center → Renters** — Add renter (name, address, phone, email).
2. **Reservations → New** — Select renter, dates/times, fee, deposit → Save as Draft → Confirm.
3. Confirm appears on **CC schedule** / shared Schedule filtered to CC.
4. **Generate agreement** — Preview PDF → Print / save to vault.
5. **Payments** — Post deposit + fee charges; record payment; **Receipt**.
6. After event — **Inspection** PostRental, mark satisfactory (or damage notes).
7. Mark reservation **Completed**.
8. **Inventory** — Adjust chair/table/kitchen counts as needed.
9. **Maintenance** — Open WO if repair needed; **Complete** with completer name.

## Dev verification

```bash
dotnet test tests/Wiley.Apartments.Tests
# Focus (once added):
dotnet test --filter FullyQualifiedName~Facility
```

## NAS note

Infrequent deploy via `./scripts/deploy-to-nas.sh` after Mac acceptance. Docs land under `/volume1/apartments/docs/community-center/`.
