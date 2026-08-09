# DS225+ capability fence — ClerkSuite

Design and implement so the app fits this host. **Do not deploy to the NAS for routine
dev loops.** Build and run on the Mac; push an image only for milestone / acceptance tests.

## Measured host (`mr-storage`, Tailscale SSH)

Polled 2026-08-09 (post RAM upgrade):

| Fact                   | Value                                                                                              |
| ---------------------- | -------------------------------------------------------------------------------------------------- |
| Model                  | Synology **DS225+**                                                                                |
| RAM (OS-reported)      | **~5.6 GiB** (~6 GB installed)                                                                     |
| Shared tenants         | TIKR (`tikr-web`, `tikr-api`, `tikr-ollama`), MailPlus/rspamd, Drive, Tailscale, Container Manager |
| Typical free/available | ~1–3 GiB available when TIKR is idle-ish; swap may already be in use                               |
| Host port **8080**     | Taken by `tikr-web` → ClerkSuite uses **8082→8080**                                                |
| Arch for images        | **linux/amd64** (build on Apple Silicon with `--platform linux/amd64`)                             |
| Docs share             | `/volume1/apartments/docs` → container `/docs`                                                     |
| DB                     | SQLite on Docker volume `clerksuite-data` (not SMB)                                                |

## Design envelope (v1)

| Constraint        | Limit                                       | Why                                              |
| ----------------- | ------------------------------------------- | ------------------------------------------------ |
| Concurrent clerks | **2**                                       | Blazor Interactive Server circuits + Syncfusion  |
| Property scale    | **~16 units**                               | Spec / SQLite single-container default           |
| App RSS target    | **≤ ~1.5 GiB**                              | Share RAM with TIKR + DSM; compose may set 1536M |
| Cold start floor  | **≥ ~512 MiB**                              | Reservation / first circuit                      |
| Database          | **SQLite default**                          | No second DB container in v1                     |
| UI stack          | Blazor Interactive Server + Syncfusion only | Constitution V                                   |
| Auth              | Identity; no role split in v1               | Two seeded clerks                                |
| Heavy AI on NAS   | **Out of scope** for ClerkSuite runtime     | `tikr-ollama` already competes for RAM           |
| Public internet   | Not required for v1                         | Tailscale / LAN first                            |

Postgres/MariaDB override is optional and **adds** ~512M+ — treat as exception, not default.

## Compose memory note

`deploy.resources.limits.memory: 1536M` is the **design target**. On Synology Container
Manager (non-Swarm) that key is often **not enforced**. Still size the app as if the
cap is real; prefer DSM/Container Manager resource caps if a hard fence is needed later.

## Dev vs NAS cadence

| Phase                                     | Where                                   | Deploy to NAS?       |
| ----------------------------------------- | --------------------------------------- | -------------------- |
| Day-to-day implement / unit / integration | Mac (`dotnet`, local Docker)            | **No**               |
| UI/E2E                                    | Mac                                     | **No**               |
| Milestone / clerk acceptance / T7.x       | Build image on Mac → `deploy-to-nas.sh` | **Yes — infrequent** |

Preferred one-shot test path: `./scripts/deploy-to-nas.sh` (see [DEPLOY.md](./DEPLOY.md)).

## Non-goals on this host

- Multi-instance / load-balanced Blazor Server
- Always-on large language models for ClerkSuite
- Storing SQLite on SMB/CIFS mounts
- Competing with TIKR for port 8080
- Frequent image churn on the NAS during feature work

## Acceptance evidence

Document observed RSS / concurrent-clerk behavior during T7.1 on this hardware.
Update this table if RAM, co-tenants, or ports change.
