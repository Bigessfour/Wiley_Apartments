# ClerkSuite — Aurora / App Runner production notes

Same Blazor Interactive Server binary (`Wiley.Apartments.Web`) runs on Synology (SQLite) or AWS (Postgres). UI and business logic are unchanged; only connection configuration differs.

## Docker images

| File | Role |
| ---- | ---- |
| [`Dockerfile`](../Dockerfile) (repo root) | App Runner / generic production build context |
| [`deploy/Dockerfile`](./Dockerfile) | Same multi-stage publish (NAS scripts / compose) |

```bash
# From repo root
docker build -t clerksuite:latest -f Dockerfile .
# or
docker build -t clerksuite:latest -f deploy/Dockerfile .
```

## Compose profiles

| Compose | Database | Use |
| ------- | -------- | --- |
| [`docker-compose.yml`](./docker-compose.yml) | **SQLite** `/data/clerksuite.db` | Local Mac smoke |
| [`synology/docker-compose.yml`](./synology/docker-compose.yml) | **SQLite** | NAS clerk workflow (default) |
| [`docker-compose.prod.yml`](./docker-compose.prod.yml) | **Postgres** (Aurora/RDS) | AWS / prod verification |
| [`docker-compose.postgres.yml`](./docker-compose.postgres.yml) | Companion Postgres container | Optional local Postgres smoke (merge with compose) |

**Do not** point Synology compose at Postgres unless town IT explicitly opts in.

## Environment variables (Aurora / RDS)

| Variable | Required | Example / notes |
| -------- | -------- | --------------- |
| `ClerkSuite__DatabaseProvider` | Yes (prod) | `Postgres` |
| `ConnectionStrings__DefaultConnection` | Yes | Npgsql string with `SSL Mode=Require` for Aurora |
| `ClerkSuite__DocumentRoot` | No | Default `/docs` |
| `SYNCFUSION_LICENSE_KEY` | Yes in practice | Runtime only; never bake into image |
| `ClerkSuite__PaymentPortalUrl` | No | Town pay-bill URL |
| `ClerkSuite__LateFeesEnabled` | No | `false` by default |
| `ASPNETCORE_ENVIRONMENT` | No | `Production` |
| `ASPNETCORE_URLS` | App Runner | Usually `http://+:8080` (Dockerfile default) |

Template: [`deploy/env.prod.example`](./env.prod.example).

### App Runner / ECS task example

```text
ClerkSuite__DatabaseProvider=Postgres
ConnectionStrings__DefaultConnection=Host=....rds.amazonaws.com;Port=5432;Database=clerksuite;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
ClerkSuite__DocumentRoot=/docs
SYNCFUSION_LICENSE_KEY=***
```

Mount or sync documents to `/docs` (EFS or S3 sync sidecar). Keep `/data` available for Data Protection keys if cookies/auth must survive restarts (or configure DP key storage for multi-instance carefully).

## Local / Synology SQLite (unchanged)

Omit `ClerkSuite__DatabaseProvider` or set `Sqlite`. Connection string remains:

```text
ConnectionStrings__DefaultConnection=Data Source=/data/clerksuite.db
```

(or `Data Source=Data/clerksuite.db` for `dotnet run` on Mac)

## Migrations caveat

Existing EF migrations were generated against SQLite. First Aurora deploy should use a **fresh** Postgres database and validate `MigrateAsync()` (or apply a provider-specific baseline). Do not assume a byte-for-byte SQLite file copy into Aurora.

## Related

- Suite cost/hosting: Wiley Widget `docs/aws-low-cost-architecture.md`
- NAS deploy: [`synology/DEPLOY.md`](./synology/DEPLOY.md)
