# Self-hosted runner (ClerkSuite CI)

Use a self-hosted Linux runner when you want:

- Local validation of GitHub Actions without burning free minutes
- Faster/repeated Ollama reviews (models stay on disk)
- Larger models (`qwen2.5-coder:3b` or `7b`) than `ubuntu-latest` can comfortably run

## Labels

Register the runner with these labels (exact match required by [workflows/ci.yml](workflows/ci.yml)):

- `self-hosted`
- `linux`
- `clerksuite-ci`

## One-time setup (Linux VM or NAS container)

1. Repo → **Settings → Actions → Runners → New self-hosted runner**
2. Follow the Linux download/configure steps from GitHub.
3. When prompted for labels, add `clerksuite-ci` (GitHub already adds `self-hosted` / OS labels).
4. Install .NET 9 SDK, PowerShell 7+, and Ollama on the machine:

   ```bash
   # .NET 9 — https://dot.net
   # PowerShell — https://aka.ms/powershell
   curl -fsSL https://ollama.com/install.sh | sh
   ollama pull qwen2.5-coder:1.5b   # or 3b/7b if the host has RAM
   ```

5. Start the runner (interactive or as a service):

   ```bash
   ./run.sh
   # or: sudo ./svc.sh install && sudo ./svc.sh start
   ```

## Run CI on the self-hosted runner

**Actions → CI → Run workflow**

| Input | Value |
| ----- | ----- |
| `use_self_hosted` | `true` |
| `ollama_model` | `qwen2.5-coder:1.5b` (or `3b` / `7b`) |

Push/PR events always use `ubuntu-latest` so contributors are not blocked if your runner is offline.

## Caching notes

On GitHub-hosted runners, the workflow caches:

- `~/.nuget/packages` + NuGet HTTP cache
- Playwright browsers (`.playwright-browsers` + `~/.cache/ms-playwright`)
- `~/.ollama` models

On self-hosted, those directories already persist across jobs; Actions cache is still useful if you wipe the work directory, but Ollama/Playwright installs are largely one-time.

## Security

- Treat the runner as a trusted machine (full repo checkout + secrets access when configured).
- Do not store Syncfusion license keys in the workflow YAML; use runner env / GitHub secrets only.
- Prefer a dedicated CI user/VM, not your daily desktop, if the runner is always-on.
