# ClerkSuite performance

Measured 2026-11-15 from the office Mac against the live Tailscale origin, before the export-auth image. Anonymous only. Signed-in page timing waits for the later audit.

| Check | Result |
| --- | --- |
| `GET https://mr-storage.tail57aa13.ts.net:8443/health` | HTTP 200, time to first byte 0.257 s |
| `GET https://mr-storage.tail57aa13.ts.net:8443/Account/Login` | HTTP 200, time to first byte 0.179 s |

Capacity target, from the NAS fence: two clerks at a time, application memory about 1.5 GiB on the DS225+. The container limit in `deploy/synology/docker-compose.yml` is `mem_limit: 1536m`.
