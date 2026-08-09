# Syncfusion Secrets & Toolchain — WileyApartments

**Rule:** Real key values MUST NOT appear in this file, Spec Kit markdown, source, or git.
Document **steps and paths** only.

## Toolchain verification (T0.0)

Run before T0.1. Record pass/fail in verification table below (no secrets in notes).

```bash
# 1. Agentic UI Builder skill (Cursor)
apm install syncfusion/blazor-ui-builder -t cursor

# 2. Component skills (global)
npx skills add syncfusion/blazor-ui-components-skills -g

# 3. MCP API key path (preferred — populate once from Keychain)
mkdir -p ~/.config/syncfusion && chmod 700 ~/.config/syncfusion
# Interactive one-time: write Keychain value to file (see READINESS.md §8.C)
export Syncfusion_API_Key_Path="$HOME/.config/syncfusion/api.key"

# 4. Test MCP in Cursor: sf_blazor_assistant — "SfGrid getting started Blazor Server"
```

**Pass criteria:** MCP responds; app or scaffold renders `SfButton`/`SfGrid` without license watermark.

Full MacBook steps: [READINESS.md §8](../../specs/001-wiley-apartment-v1/READINESS.md).

---

## Two keys (Keychain is source of truth)

| Variable                                         | Used by                        | Dev delivery                                          | NAS                                     |
| ------------------------------------------------ | ------------------------------ | ----------------------------------------------------- | --------------------------------------- |
| `SYNCFUSION_LICENSE_KEY`                         | Blazor app (`RegisterLicense`) | Keychain → user-secrets or env                        | Container env / gitignored secrets file |
| `Syncfusion_API_Key` / `Syncfusion_API_Key_Path` | Cursor MCP only                | Keychain → `~/.config/syncfusion/api.key` (preferred) | **Not needed**                          |

### API key delivery (MCP — pick one)

1. **Preferred:** `Syncfusion_API_Key_Path=$HOME/.config/syncfusion/api.key` (mode 600, outside repo)
2. **Keychain bridge:** `~/.cursor/scripts/run-sf-blazor-mcp.sh`
3. **Session env:** `export Syncfusion_API_Key=...` (never commit)

---

## Runtime license registration

In `Program.cs` — **never hardcode**:

```csharp
using Syncfusion.Licensing;

var key = builder.Configuration["SYNCFUSION_LICENSE_KEY"]
    ?? builder.Configuration["Syncfusion:LicenseKey"];
if (!string.IsNullOrWhiteSpace(key))
{
    SyncfusionLicenseProvider.RegisterLicense(key.Trim());
    SyncfusionLicenseProvider.ValidateLicense(Platform.Blazor, out _);
}
```

Populate via `scripts/setup-local-secrets.sh` (T0.1) reading Keychain into dotnet user-secrets.

---

## NAS production

- DSM → Container Manager → Environment → `SYNCFUSION_LICENSE_KEY`
- Or mount gitignored `/volume1/apartments/secrets/clerksuite.env`
- **Never** in committed docker-compose or git
- MCP API key is **dev-only** — not deployed to NAS

---

## Verification log (fill at T0.0 — no key values)

| Check                                               | Date       | Pass |
| --------------------------------------------------- | ---------- | ---- |
| Blazor UI Builder skill installed (`apm install`)   | 2026-08-09 | Y    |
| Component skills installed (67+)                    | 2026-08-09 | Y    |
| Keychain bridge configured (`run-sf-blazor-mcp.sh`) | 2026-08-09 | Y    |
| sf_blazor_assistant responds                        | 2026-08-09 | Y    |
| Sf* component no watermark                          | 2026-08-09 | Y    |
| Keys not in repo (`git grep` clean)                 | 2026-08-09 | Y    |

---

## Forbidden

- Real keys in Spec Kit files, tracked source, PRs, docker-compose values, or chat
- Hardcoded `RegisterLicense("...")` in source
