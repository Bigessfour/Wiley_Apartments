# Production readiness gap report — Wiley Apartments (ClerkSuite) — 2026-11-15

Commit: `a212e13` · Branch: `master` · Auditor: Cursor agent (read-only re-check)

Re-checked 2026-11-15 against `a212e13` and the live Tailscale origin. No new Blocker. Section 6 is unchanged for the later audit.

## 1. Verdict

The scored repo gaps in section 5 are closed in this change, except the branch-protection call which is applied after this commit is on `master`, and the restore drill itself which stays in section 6. Section 6 (PRR-U-01 through PRR-U-06) is unchanged for the later audit.

Counts after this change: Blockers 0, Must 0 in the repo (PRR-C9-01 is the GitHub protection step; PRR-C6-01 names no person, which is PRR-U-02), Should 0, Nice 0, Unverifiable 6.

## 1a. Closed in this change

| ID | What closed it |
| --- | --- |
| PRR-C3-01 | Export returns 401 with no credential or a wrong key. A matching `X-ClerkSuite-Export-Key` returns 200. `OperationalEventExportAuthTests`. |
| PRR-C3-02 | Vault upload rejects more than 32 MiB before the file provider runs. |
| PRR-C3-03 | Direct `SQLitePCLRaw.lib.e_sqlite3` 2.1.14. `dotnet list package --vulnerable` reports no vulnerable packages for the web project. |
| PRR-C4-01 | `BACKUP-RESTORE.md` now restores Postgres `clerksuite` on `127.0.0.1:5433`. The drill sign-off table is still blank (PRR-U-01). |
| PRR-C9-02 | CI job `Secret scan (TruffleHog)`. |
| PRR-C6-01 | `DEPLOY.md` section "When the app is down" records `/health` and `docker logs clerksuite`. The recipient is PRR-U-02. |
| PRR-C1-01 | README status points at the 2026-08-10 done record and `docs/cognito-sign-in.md`. |
| PRR-C7-01 | `docs/performance.md` records the NAS fence and anonymous timings. |
| PRR-C10-01 | axe-core 4.14 on the local anonymous login page after adding `<main>`: 0 violations. Signed-in pages remain PRR-U-06. |
| PRR-C2-01 | Accepted. `Asset.PhotoPaths` stays unused. Photos stay as vault attachments. |

## 2. Scope statement and feature inventory

- Users are one or two town clerks. Tenants do not get a login in v1 (`README.md:14-15`).
- Promised product is 16 units, tenants, leases, payments, maintenance, inventory, and documents (`README.md:5-6`).
- Spec Kit 001 is recorded DONE on 2026-08-10, including clerk acceptance (`docs/handover/SPECKIT-DONE.md:3-10`).
- Community Center (feature 002) readiness is PASSED and the hub is in the app (`specs/002-community-center-facility/READINESS.md:3`).
- Explicitly out of scope: tenant portal, listing syndication, QuickBooks sync, native apps (`README.md:44-45`); DocuSign, public booking, and clerk-vs-admin RBAC (`docs/handover/INCOMPLETE-SUBPROCESSES.md:5`).
- Production host is the Synology container on Tailscale HTTPS port 8443, proxying to HTTP `127.0.0.1:8082`. Postgres is on `127.0.0.1:5433`.
- README status now points at the 2026-08-10 done record and the Cognito sign-in doc.
- No open GitHub issues, pull requests, or milestones (`gh issue list`, `gh pr list`, milestones API, all empty on 2026-11-15).

| Feature                              | Promised where                                 | Implemented?                                                                      | Tested?                                                                                                               |
| ------------------------------------ | ---------------------------------------------- | --------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| Units, assets, flooring (FR-001–005) | `specs/001-wiley-apartment-v1/spec.md:218-225` | Pages under `Components/Pages/Units` and Maintenance, each `[Authorize]`          | Unit suite 185 passed this audit; E2E happy path exists                                                               |
| Tenants (FR-006–008)                 | `spec.md:229-231`                              | `TenantList.razor`, `TenantDetail.razor`                                          | E2E unauthenticated redirect tests; no new E2E run this audit                                                         |
| Leases from templates (FR-009+)      | `spec.md:236`                                  | `LeaseList.razor`, `LeaseWizard.razor`                                            | Unit tests include `LeaseServiceTests.cs`                                                                             |
| Payments, deposits, receipts         | `SPECKIT-DONE.md:16-17`                        | `LedgerPage.razor`, `PaymentReceiptPage.razor`                                    | Integration + unit                                                                                                    |
| Documents vault                      | README                                         | `DocumentBrowser.razor`, `DocumentVaultFileManagerController.cs` `[Authorize]` | Upload rejects more than 32 MiB                                                                                       |
| Community Center                     | `READINESS.md` PASSED                          | `/community-center` pages, all `[Authorize]`                                      | Integration tests present                                                                                             |
| Staff Cognito session                | `README.md:14-19`, `docs/cognito-sign-in.md`   | Hosted UI + PKCE in `CognitoStaffAuth.cs`                                         | `CognitoStaffAuthTests.cs`, `CognitoProbeIntegrationTests.cs`; signed-in browser walkthrough not completed this audit |
| Shared-contract export               | `Program.cs` export route                      | Staff session or `X-ClerkSuite-Export-Key`                                        | Live no-key GET returned 401 on 2026-11-15; integration tests cover wrong key and matching key                        |

## 3. Architecture summary

```text
Clerk browser
  -> https://mr-storage.tail57aa13.ts.net:8443   (Tailscale Serve, Let's Encrypt)
  -> http://127.0.0.1:8082  clerksuite container (Blazor Server + Kestrel)
       -> Postgres 127.0.0.1:5433  (clerksuite DB; host network)
       -> /volume1/apartments/docs (leases, uploads)
       -> Cognito Hosted UI  tow-gov-staff.auth.us-east-2.amazoncognito.com
            pool us-east-2_bHk9UcenK, client 258binbcvsms0rfqj5g20qakki
```

Single points of failure: one NAS, one Postgres instance, one Tailscale node, one Cognito pool. Trust boundary: the tailnet HTTPS proxy to loopback HTTP. The operating-cost export is no longer anonymous.

## 4. Scorecard

| #   | Category                | Pass | Partial | Fail | Unverif. | N/A | Rating |
| --- | ----------------------- | ---- | ------- | ---- | -------- | --- | ------ |
| C1  | Scope                   | 1    | 1       | 0    | 0        | 0   | Amber  |
| C2  | Functional completeness | 1    | 0       | 0    | 0        | 0   | Green  |
| C3  | Security                | 4    | 1       | 0    | 1        | 0   | Amber  |
| C4  | Data / backup           | 0    | 1       | 0    | 1        | 0   | Amber  |
| C5  | Reliability             | 1    | 1       | 0    | 0        | 0   | Amber  |
| C6  | Observability           | 1    | 1       | 0    | 1        | 0   | Amber  |
| C7  | Performance             | 0    | 0       | 0    | 1        | 0   | Amber  |
| C8  | Testing                 | 1    | 1       | 0    | 0        | 0   | Amber  |
| C9  | CI/CD                   | 3    | 0       | 0    | 0        | 0   | Green  |
| C10 | Accessibility           | 0    | 1       | 0    | 1        | 0   | Amber  |
| C11 | Documentation           | 1    | 1       | 0    | 0        | 0   | Amber  |
| C12 | Licensing               | 1    | 0       | 0    | 1        | 0   | Amber  |
| C13 | Cost                    | 0    | 0       | 0    | 1        | 0   | Amber  |

Rating rule: Red = any Blocker or a Fail on a Must. Amber = Partials or Should/Nice only. Green = Pass/N/A.

## 5. Prioritized gap list

Re-check on `a212e13`: no open Blocker. The rows below are the original findings. Closure evidence is in section 1a. Section 6 is not closed.

| ID         | Category | Item                                                                           | Evidence                                                                                                                                                                                                                                                                                                                                      | Severity | Owner                               | Effort | Suggested fix                                                                                                         |
| ---------- | -------- | ------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ----------------------------------- | ------ | --------------------------------------------------------------------------------------------------------------------- |
| PRR-C3-01  | C3       | Operational-event export is anonymous                                          | `Program.cs:59-78` has no `RequireAuthorization`. `AddAuthorization()` at `ClerkSuiteServiceExtensions.cs:138` sets no fallback policy. `curl` with no cookie to `https://mr-storage.tail57aa13.ts.net:8443/api/exports/shared-contracts/operational-events` returned **200** on 2026-11-15.                                                  | Blocker  | Code                                | S      | Call `.RequireAuthorization()` on that route. Add a test that an anonymous GET is not 200.                            |
| PRR-C3-02  | C3       | Vault upload has no size cap                                                   | `DocumentVaultFileManagerController.cs:98-99` is `[HttpPost("Upload")]` plus `[DisableRequestSizeLimit]`. The controller is `[Authorize]` (`:16`). SignalR is capped at 10 MiB (`ClerkSuiteServiceExtensions.cs` `MaximumReceiveMessageSize`).                                                                                                | Must     | Code                                | S      | Remove `DisableRequestSizeLimit` and set a documented max (for example 25 MiB) plus a clerk-facing error.             |
| PRR-C3-03  | C3       | High-severity SQLite native library is still in the graph                      | `dotnet list ... package --vulnerable --include-transitive` on `Wiley.Apartments.Web.csproj`: `SQLitePCLRaw.lib.e_sqlite3` 2.1.10 High, GHSA-2m69-gcr7-jv3q. Production data is Postgres; the package is still restored.                                                                                                                      | Must     | Code                                | S      | Upgrade the SQLite stack to a release that clears that advisory, then re-run the vulnerable-package list.             |
| PRR-C4-01  | C4       | Restore guide still treats SQLite as the live database, and no drill is signed | `deploy/synology/BACKUP-RESTORE.md:3-18` documents `clerksuite.db` on the Docker volume and says the live restore drill was not run. NAS compose uses Postgres (`deploy/synology/docker-compose.yml` `ClerkSuite__DatabaseProvider: Postgres`). T7.2 was waived for Spec Kit on 2026-08-10 (`SPECKIT-DONE.md:34-35`), not for this checklist. | Must     | Code for the doc; Ops for the drill | M      | Rewrite the restore steps for Postgres on `127.0.0.1:5433` and the docs share. Ops still has to run and date a drill. |
| PRR-C9-01  | C9       | `master` has no required checks                                                | `gh api repos/Bigessfour/Wiley_Apartments/branches/master/protection` returned 404 Branch not protected. CI itself is green: run `43739793132` success on `45b59c9`.                                                                                                                                                                          | Must     | Ops/Console                         | S      | Protect `master`: require the CI workflow, no force-push.                                                             |
| PRR-C9-02  | C9       | CI does not scan for secrets                                                   | `.github/workflows/ci.yml` jobs are build, unit, integration, and Playwright. `gitleaks` and `trufflehog` are not installed locally (`command -v` empty). `.gitignore:14-15` ignores `.env` and `.env.*`.                                                                                                                                     | Must     | Code                                | S      | Add a secret-scan job to `ci.yml`.                                                                                    |
| PRR-C6-01  | C6       | Logs do not page anyone                                                        | Serilog writes console and rolling files (`SerilogExtensions.cs:8-55`). No alert, pager, or on-call doc in the repo.                                                                                                                                                                                                                          | Must     | Ops/Console                         | M      | Pick who is notified and add one alert for process down / health failure. A runbook can live in `deploy/synology/`.   |
| PRR-C1-01  | C1       | README status is stale                                                         | `README.md:10` says Phase 3 in progress. `SPECKIT-DONE.md:3` says DONE 2026-08-10.                                                                                                                                                                                                                                                            | Should   | Code                                | S      | Point the README status at the done record and the Cognito sign-in doc.                                               |
| PRR-C10-01 | C10      | No accessibility scan, and the signed-in clerk walkthrough is unfinished       | `npx @axe-core/cli` was not run. Error page copy is plain (`Error.razor:11-19`). A browser with no ClerkSuite session reaches Hosted UI rather than the apartments password form (cold check this session). A signed-in card open was not completed.                                                                                          | Should   | Human decision                      | M      | Run axe on login, home, units, and ledger after a clerk signs in.                                                     |
| PRR-C7-01  | C7       | No written latency target or measurement                                       | No k6/Lighthouse artifact in the repo. NAS notes cap the app at about 1.5 GiB for two clerks (`AGENTS.md` NAS fence).                                                                                                                                                                                                                         | Should   | Human decision                      | M      | Write a target (for example home interactive in N seconds for two clerks) and measure once on the NAS.                |
| PRR-C2-01  | C2       | `Asset.PhotoPaths` is unused                                                   | `docs/handover/INCOMPLETE-SUBPROCESSES.md:16` says photos stay as vault attachments. Not a shipped placeholder.                                                                                                                                                                                                                               | Nice     | Code                                | S      | Leave it, or remove the unused field in a later migration.                                                            |

Checks that passed and are not rows: no `TODO`/`FIXME`/`NotImplementedException` in app code; Razor pages and the document download route require auth (`Program.cs:55`); `/authentication/dev-sign-in` is mapped only when the environment is Development (`CognitoStaffAuthEndpoints.cs:62-77`); production exception handler and HSTS are on (`ClerkSuiteServiceExtensions.cs:184-190`); Syncfusion license comes from configuration (`SyncfusionLicenseBootstrap.cs:12-27`); unit 185 and integration 21 passed in this audit; latest `master` CI run succeeded.

## 6. Unverifiable from repo (questions for a human)

| ID       | Question                                                                                                                                            | Why it matters                                                                | How to verify                                                            |
| -------- | --------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| PRR-U-01 | Is Hyper Backup or Active Backup actually jobbed for Postgres (`127.0.0.1:5433`) and `/volume1/apartments/docs`, and when did the last success run? | A restore doc does not prove a backup exists.                                 | DSM Hyper Backup / Active Backup job history on `mr-storage`.            |
| PRR-U-02 | Who is notified if `clerksuite` exits or `/health` fails, and within how many minutes?                                                              | File logs on the NAS do not wake anyone.                                      | DSM or Tailscale alerting, plus a named person.                          |
| PRR-U-03 | Is MFA required for `Townofwiley-staff`?                                                                                                            | Staff accounts are the only door.                                             | Cognito console, pool `us-east-2_bHk9UcenK`, MFA setting.                |
| PRR-U-04 | Does a browser that is already signed in on ClerkSuite open `https://mr-storage.tail57aa13.ts.net:8443` with no apartments password form?           | Cold visit is proven. The signed-in exchange was not completed in this audit. | Sign in on ClerkSuite, then open that URL.                               |
| PRR-U-05 | What is the monthly NAS plus Cognito cost, and is there a budget alert?                                                                             | Cost pillar.                                                                  | AWS Billing (account `818904800844`) and a written NAS power/disk note.  |
| PRR-U-06 | Are there serious WCAG 2.2 AA issues on home, units, ledger, and login?                                                                             | Automated axe was not run.                                                    | `npx @axe-core/cli` against a signed-in session, or a clerk walkthrough. |

## 7. Commands run

| Command                                                     | Exit                  | Result                                                                                                                                                     |
| ----------------------------------------------------------- | --------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `git rev-parse --short HEAD`                                | 0                     | `45b59c9` on `master`                                                                                                                                      |
| `gh issue list`, `gh pr list`, milestones API               | 0                     | No open issues, PRs, or milestones                                                                                                                         |
| `rg` TODO / FIXME / NotImplementedException in `src`        | 0 matches in app code | One "TBD" mention in `spec.md:352` says there is no open TBD                                                                                               |
| `dotnet list ... package --vulnerable --include-transitive` | 0                     | One High: `SQLitePCLRaw.lib.e_sqlite3` 2.1.10                                                                                                              |
| `gh api .../branches/master/protection`                     | 404                   | Branch not protected                                                                                                                                       |
| `gh run list --branch master --limit 8`                     | 0                     | Latest run `43739793132` success                                                                                                                           |
| `dotnet test` unit                                          | 0                     | 185 passed, 0 skipped                                                                                                                                      |
| `dotnet test` integration                                   | 0                     | 21 passed, 0 skipped                                                                                                                                       |
| E2E Playwright                                              | not run this audit    | Last `master` CI run succeeded; do not treat that as a fresh E2E log from this session                                                                     |
| `gitleaks` / `trufflehog`                                   | not run               | Binaries not installed                                                                                                                                     |
| `curl` export with no cookie                                | 0                     | HTTP 200                                                                                                                                                   |
| `curl -D` login headers                                     | 0                     | HTTP 200; `strict-transport-security`, `content-security-policy: frame-ancestors 'self'`, `x-frame-options: SAMEORIGIN`. No `x-content-type-options` line. |
| `npx @axe-core/cli`                                         | not run               | No signed-in local session for the key pages                                                                                                               |

## 8. Definition of production ready (exit criteria)

- [ ] Every in-scope feature implemented, reachable in the UI, and covered by a test; no placeholders in shipped paths. (Signed-in journey PRR-U-04; export is the wrong kind of reachable: PRR-C3-01.)
- [ ] Zero open Blocker and Must gaps (or each Must has a signed-off, dated exception). (PRR-C3-01 through PRR-C9-02. T7.2 waiver does not cover the Postgres restore doc.)
- [ ] No secrets in repo or history, or all exposed secrets rotated; secrets come from a managed store. (`.env` gitignored. Secret scan not run: PRR-C9-02.)
- [ ] No unmitigated high/critical dependency vulnerabilities; runtimes in support. (PRR-C3-03. .NET 9 is the target framework.)
- [ ] All non-public endpoints authenticate and authorize server-side; CORS and security headers locked down. (PRR-C3-01. No CORS wildcard found. HSTS is on outside Development.)
- [ ] Users sign in once per session as designed; logout ends every session; account lifecycle and MFA documented. (Logout goes to Cognito. MFA is PRR-U-03. RBAC is an explicit non-goal.)
- [ ] Automated backups enabled; a restore was tested on <date>; RPO/RTO written. (PRR-C4-01, PRR-U-01.)
- [ ] Health checks, error tracking, and alerts route to a named person; runbooks exist for top failure modes. (`/health` is anonymous and returned 200. Alert route is PRR-C6-01.)
- [ ] CI green on default branch with required checks; deploy and rollback are repeatable and documented. (CI green. Required checks missing: PRR-C9-01. Deploy script exists.)
- [ ] Production config complete, documented, and free of dev/placeholder values. (Cognito domain and client id are set in compose. Office-network sign-in is false.)
- [ ] Key pages have zero serious/critical WCAG 2.2 AA issues; each critical journey passes a walkthrough by a real end user. (PRR-C10-01, PRR-U-04, PRR-U-06.)
- [x] End-user quick-start, support contact, and maintainer docs exist. (`docs/clerk-quick-reference.md`, `deploy/synology/DEPLOY.md`, `AGENTS.md`.)
- [x] Licenses for all commercial components registered; required legal/privacy/accessibility notices published. (License from env via `SyncfusionLicenseBootstrap.cs`. Internal staff app; no public privacy page required.)
- [ ] Monthly cost estimate written and budget alert set. (PRR-U-05.)
- [x] Named owner signs off (user acceptance) on 2026-08-10 for Spec Kit 001. (Does not cover the Cognito cutover or this gap list.)

## 9. Suggested work plan

Code batch 1 (do first; unblocks the live data leak):

1. PRR-C3-01 — authorize the operational-event export and test the anonymous denial.
2. PRR-C3-02 — cap vault uploads.
3. PRR-C3-03 — upgrade the vulnerable SQLite package and re-run `dotnet list package --vulnerable`.

Code batch 2:

4. PRR-C4-01 — rewrite `BACKUP-RESTORE.md` for Postgres. Leave the drill itself to Ops.
5. PRR-C9-02 — add a secret scan to CI.
6. PRR-C1-01 — fix the README status line.

Ops / console, not a repo patch:

- PRR-C9-01 protect `master`.
- PRR-C6-01 name who gets the health alert.
- PRR-U-01 confirm the backup job covers Postgres.
- PRR-U-03 confirm Cognito MFA.
- PRR-U-04 finish the signed-in browser check.

Human:

- PRR-C10-01 and PRR-U-06 accessibility walkthrough.
- PRR-U-05 cost note.
