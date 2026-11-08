# Shared-contracts export → Wiley Widget

**Why Apartments (not Trash Truck):** ClerkSuite already stores landlord operating costs (`UnitOperatingCost`: utility, repair, replace, common upkeep, renovation). Trash Truck today is routes/scale tickets — no fuel/repairs/disposal ledger to map without inventing data.

This export is **read-only**. Rent ledger, P/L UI, and cost CRUD are unchanged.

## Endpoint

```http
GET /api/exports/shared-contracts/operational-events?period=2026-10
```

Returns a bare **`OperationalEvent[]`** (Widget ingest shape — not a PayloadBatch envelope).

| Field | Value |
| ----- | ----- |
| `source` | `other` (closed schema enum; publisher in metadata) |
| `type` | `check` (positive outflow; not a fictional `cost` type) |
| `metadata.enterpriseId` | `tow-apartments` |
| `metadata.publisherSystem` | `wiley-apartments` |
| `metadata.costCategory` | enum name (`Repair`, `Utility`, …) |

## POST to Wiley Widget

Widget expects the same array:

```bash
# 1) Export from Apartments
curl -s "http://127.0.0.1:5077/api/exports/shared-contracts/operational-events?period=2026-10" \
  -o /tmp/apartments-events.json

# 2) Ingest into Widget
curl -s -w "\n%{http_code}\n" -X POST "http://127.0.0.1:5231/api/reference-data/events" \
  -H "Content-Type: application/json" \
  --data-binary @/tmp/apartments-events.json
# expect HTTP 202
```

One-liner script (from this repo):

```bash
PERIOD=2026-10 ./scripts/post-operating-costs-to-widget.sh
```

Env overrides: `APARTMENTS_BASE`, `WIDGET_API` / `WILEY_WORKSPACE_API_BASE_ADDRESS`.

## Optional S3 drop (Terraform bucket)

Wiley Widget Terraform (`wiley-co-web/infra/terraform`) can create a private bucket tagged `Project=tow-suite`. After apply:

```bash
BUCKET=$(cd ../Wiley-Widget\(Blazor\)/wiley-co-web/infra/terraform && terraform output -raw contracts_bucket_name)
curl -s "${APARTMENTS_BASE:-http://127.0.0.1:5077}/api/exports/shared-contracts/operational-events?period=${PERIOD:-2026-10}" \
  | aws s3 cp - "s3://${BUCKET}/shared-contracts/events/apartments/${PERIOD:-2026-10}.json" --profile tow
```

Tear the suite footing down with `infra/terraform/scripts/destroy.sh` if costs climb (does not delete existing Widget App Runner/Aurora).

## Note on the Trash “cost” example

Shared-contracts v1 does **not** define `source=trash-truck` or `type=cost`. Valid enums are in `operational-event.schema.json`. Put system identity in `metadata.publisherSystem` and use `type=check` or `type=summary` for rollups.
