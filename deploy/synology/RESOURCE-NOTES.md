# DS225+ resource notes — ClerkSuite

| Resource               | Default compose | Notes                       |
| ---------------------- | --------------- | --------------------------- |
| App memory limit       | 1536M           | Blazor Server + Syncfusion  |
| App memory reservation | 512M            | Minimum for cold start      |
| SQLite                 | `/data` volume  | Single container default    |
| Concurrent users       | 2 staff         | Acceptable at 16-unit scale |

**Upgrade path:** 6 GB NAS RAM recommended before production (T0.5 / T7.1).

Postgres override adds second container (+512M limit on DB service).

Document observed usage during T7.1 clerk acceptance on real hardware.
