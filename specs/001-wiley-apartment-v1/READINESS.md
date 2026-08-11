# Spec Kit Readiness Gate — WileyApartments / ClerkSuite

**Feature**: `001-wiley-apartment-v1` | **Date**: 2026-08-09

**Purpose**: Confirm spec-kit is fully in place and the path is clear **before** `/speckit-implement` (**T0.0**). No application code until this gate passes.

**Gate status**: **PASSED** — 2026-08-09 (G1–G7 locked; G8–G11 deferred/closed per Decision 23)

---

## 1. Spec Kit artifact checklist

| Step                   | Artifact                               | Path                                     | Status                    |
| ---------------------- | -------------------------------------- | ---------------------------------------- | ------------------------- |
| Init                   | Spec Kit scaffold                      | `.specify/`, `.cursor/skills/speckit-*`  | **Done**                  |
| Constitution           | Governance                             | `.specify/memory/constitution.md` v1.2.0 | **Done**                  |
| Specify                | Feature spec + FR acceptance           | `specs/001-wiley-apartment-v1/spec.md`   | **Done**                  |
| Specify QA             | Requirements checklist                 | `checklists/requirements.md`             | **Done**                  |
| Plan                   | Stack, architecture, deployment, risks | `plan.md`                                | **Done**                  |
| Plan (depth)           | Research decisions (23)                | `research.md`                            | **Done**                  |
| Plan (depth)           | Entity model                           | `data-model.md`                          | **Done**                  |
| Plan (depth)           | Service contracts                      | `contracts/services.md`                  | **Done**                  |
| Plan (depth)           | Clerk acceptance script                | `quickstart.md`                          | **Done**                  |
| Tasks                  | Phases 0–7 (T0.0–T7.4)                 | `tasks.md`                               | **Done**                  |
| Analyze                | Cross-artifact report                  | `checklists/analyze-report.md`           | **Done**                  |
| Feature pointer        | Active feature dir                     | `.specify/feature.json`                  | **Done**                  |
| Git remote             | GitHub (no push yet)                   | `origin` → Bigessfour/Wiley_Apartments   | **Configured**            |
| AGENTS.md              | Syncfusion MCP UI rules                | `AGENTS.md`                              | **Done**                  |
| Syncfusion secrets doc | Key handling + T0.0 verification       | `deploy/synology/SYNCFUSION-SECRETS.md`  | **Done** (verify at T0.0) |

### Optional (not required to start implement)

| Item                       | Status       | Notes                                           |
| -------------------------- | ------------ | ----------------------------------------------- |
| `/speckit-clarify` session | Not run      | G1–G7 resolved without clarify session          |
| Git feature branch         | Not created  | `001-wiley-apartment-v1` optional until T0.1    |
| Initial commit / push      | **Deferred** | Per project policy until full workup signed off |

---

## 2. Workup processing summary

- **Constitution** — 8 principles + spec-kit governance + Syncfusion mandate v1.2.0
- **Spec** — ClerkSuite FR-1–FR-7 + user stories + FR acceptance checkboxes
- **Plan** — stack, architecture, locked configuration defaults (G1–G7)
- **Tasks** — Phases 0–7; **T0.0** Syncfusion toolchain before T0.1
- **Syncfusion mandate** — mandatory MCP/Agentic/skills; Keychain-only keys

**Working names**: **WileyApartments** (repo/solution) · **ClerkSuite** (UI brand)

---

## 3. Cross-artifact consistency (analyze summary)

See full report: [checklists/analyze-report.md](./checklists/analyze-report.md)

**Verdict**: Plan, spec, tasks, and data model align on locked decisions below.

---

## 4. Product / environment decisions — LOCKED (G1–G7)

| #      | Decision           | Resolution                                                                                                      | Task        |
| ------ | ------------------ | --------------------------------------------------------------------------------------------------------------- | ----------- |
| **G1** | Location & units   | **Town of Wiley, CO**; seed **Unit 1–16 placeholders** until real list supplied                                 | T1.1        |
| **G2** | Late fees          | Settings **toggle default OFF**; when enabled, configurable **amount + grace days**; assess via ledger          | T4.2        |
| **G3** | Payment portal     | **`PaymentPortalUrl`** → Town PayStar (`secure.paystar.io` / townofwiley.gov pay-bill); deep-link only          | T4.3        |
| **G4** | Database           | **SQLite single-container production default**; Postgres/MariaDB **optional override only**                     | T0.2        |
| **G5** | Syncfusion license | **Community license** (full access); Keychain → env process (Decision 17)                                       | T0.0 / T0.1 |
| **G6** | NAS access         | **Tailscale + SSH** for dev deploy and T0.3 volume setup                                                        | T0.3        |
| **G7** | Auth / roles       | **No role differentiation**; login required for audit; **1–2 full-access accounts**; no Clerk/ReadOnly/Elevated | T0.4        |

Authoritative detail: [research.md](./research.md) Decisions 4, 9, 10, 17, 20–23.

---

## 5. Deferred items (do not block T0.0)

| #   | Item                                   | Status     | When                                 |
| --- | -------------------------------------- | ---------- | ------------------------------------ |
| G8  | Colorado lease template counsel review | Deferred   | T3.2 — Brookside fillable PDF on NAS |
| G9  | Spreadsheet data import                | Deferred   | Manual entry v1                      |
| G10 | App URL / hostname                     | Deferred   | T0.2, T7.1 — LAN / Tailscale first   |
| G11 | Account naming                         | **Closed** | Merged with G7 — passwords at deploy |

---

## 6. Process gaps

| #   | Gap                 | Status                                     |
| --- | ------------------- | ------------------------------------------ |
| P1  | User sign-off       | **Passed** — planning gate 2026-08-09      |
| P2  | G1–G11 in artifacts | **Done** — research Decision 23 + this doc |
| P3  | First git commit    | Deferred until user requests               |
| P4  | T7.4 converge/done  | After implement                            |

---

## 7. Planning gate verdict

| Criterion                                                        | Met?       |
| ---------------------------------------------------------------- | ---------- |
| Constitution exists and drives artifacts                         | Yes        |
| Spec stable with FR acceptance criteria                          | Yes        |
| Plan complete (stack, architecture, entities, deployment, risks) | Yes        |
| Tasks with Done when per phase                                   | Yes        |
| Path to implement clear                                          | Yes        |
| Product/env questions G1–G7 answered                             | **Yes**    |
| User sign-off                                                    | **Passed** |

**Next step**: `/speckit-implement` starting **T0.0** (Syncfusion toolchain verification).

```text
NOW
  └─ T0.0 Syncfusion toolchain → T0.1 scaffold → Phase 0 on NAS (Tailscale + SSH)

NOT YET
  └─ git push to GitHub
  └─ Phase 1+ until Phase 0 checkpoint passes on NAS
```

---

## 8. Developer machine setup (local only)

**Purpose:** Steps for your MacBook only. **Never commit key values** to Spec Kit files, source, or git.

### A. Store keys in Keychain (one-time)

1. Open **Passwords** (Keychain Access).
2. Create or confirm two entries (names are examples — use your existing TIKR-style labels if present):
   - **License key** — generic password, e.g. service `SYNCFUSION_LICENSE_KEY` / account `SYNCFUSION` (or `com.townofwiley.clerksuite` / `SYNCFUSION_LICENSE_KEY`)
   - **API key (MCP)** — generic password, e.g. service `com.wileyco.syncfusion.blazor-mcp` / account `SYNCFUSION_API_KEY`
3. Paste each value **only** into Keychain. Do not paste into chat, README, or Spec Kit markdown.

### B. Runtime license → Blazor app (T0.0 / T0.1)

1. At T0.1, create `scripts/setup-local-secrets.sh` (TIKR pattern) to read license from Keychain.
2. Sync into .NET User Secrets for the web project:

```bash
# After setup-local-secrets.sh exists — script reads Keychain; you do NOT paste the key here
./scripts/setup-local-secrets.sh --license-only
```

3. Or set env for a session (optional):

```bash
export SYNCFUSION_LICENSE_KEY="$(security find-generic-password -s 'SYNCFUSION_LICENSE_KEY' -a 'SYNCFUSION' -w 2>/dev/null || security find-generic-password -s 'com.townofwiley.clerksuite' -a 'SYNCFUSION_LICENSE_KEY' -w)"
```

4. `Program.cs` reads configuration and calls `RegisterLicense` — never hardcode the string.

### C. MCP / Agentic API key → Cursor (T0.0)

**Preferred — file outside repo:**

```bash
mkdir -p ~/.config/syncfusion
chmod 700 ~/.config/syncfusion
# Copy API key from Keychain into file once (interactive — do not commit this file):
security find-generic-password -s 'com.wileyco.syncfusion.blazor-mcp' -a 'SYNCFUSION_API_KEY' -w > ~/.config/syncfusion/api.key
chmod 600 ~/.config/syncfusion/api.key
export Syncfusion_API_Key_Path="$HOME/.config/syncfusion/api.key"
```

Add to your shell profile or ensure `~/.cursor/scripts/run-sf-blazor-mcp.sh` exports `Syncfusion_API_Key_Path`.

**Alternates (also not committed):**

- `run-sf-blazor-mcp.sh` reads Keychain directly (already on this machine)
- `Syncfusion_API_Key` in machine-local Cursor MCP env (not in repo `.cursor/mcp.json`)

### D. Verify (T0.0 done criteria)

| Check          | How                                                                                   |
| -------------- | ------------------------------------------------------------------------------------- |
| No keys in git | `git grep -i syncfusion.*key` and `git grep RegisterLicense` return nothing sensitive |
| MCP works      | Cursor → `sf_blazor_assistant` returns Blazor doc guidance                            |
| License works  | Local app renders `SfButton`/`SfGrid` without watermark                               |
| Toolchain      | UI Builder skill + component skills installed                                         |

Log pass/fail in [deploy/synology/SYNCFUSION-SECRETS.md](../../../deploy/synology/SYNCFUSION-SECRETS.md) verification table — **dates only, no key values**.

### E. Local run environment (Mac) — Development only

| Do                                                                                                                       | Don't                                                                                                            |
| ------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------- |
| `./scripts/run-local.sh` or `dotnet run` with launch profile **http** / **https** (`ASPNETCORE_ENVIRONMENT=Development`) | Set `ASPNETCORE_ENVIRONMENT=Production` on the Mac for day-to-day debugging                                      |
| Kill leftover listeners before relaunch (`run-local.sh` does this for port **5077**)                                     | Stack multiple `dotnet run` instances on the same port                                                           |
| Use **Production** only for published output / NAS Docker (`deploy/synology`)                                            | Expect NuGet `_content/Syncfusion.*` assets to resolve from source without Development (or `UseStaticWebAssets`) |

**Why:** Production + `dotnet run` from the project tree historically caused hundreds of `FileNotFoundException`s for Syncfusion static assets and scoped CSS. `Program.cs` calls `UseStaticWebAssets()` as a safety net and logs a warning if Production is detected under the source tree — still prefer Development locally.

Default local URL: `http://localhost:5077` (see `Properties/launchSettings.json`). E2E tests use `127.0.0.1:5199` separately.
