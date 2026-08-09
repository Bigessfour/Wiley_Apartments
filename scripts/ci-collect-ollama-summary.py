#!/usr/bin/env python3
"""Build a tight, evidence-only CI summary for the Ollama review agent."""

from __future__ import annotations

import glob
import os
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def _text(el: ET.Element | None) -> str:
    if el is None or not el.text:
        return ""
    return re.sub(r"\s+", " ", el.text).strip()


def main() -> int:
    out_dir = Path(sys.argv[1] if len(sys.argv) > 1 else "review-context")
    out_dir.mkdir(parents=True, exist_ok=True)

    build = os.environ.get("BUILD_RESULT", "unknown")
    e2e = os.environ.get("E2E_RESULT", "unknown")
    model = os.environ.get("OLLAMA_MODEL", "unknown")
    repo = os.environ.get("GITHUB_REPOSITORY", "")
    ref = os.environ.get("GITHUB_REF", "")
    sha = os.environ.get("GITHUB_SHA", "")
    event = os.environ.get("GITHUB_EVENT_NAME", "")

    lines = [
        "## CI Run Context",
        f"- Repo: {repo}",
        f"- Ref: {ref}",
        f"- SHA: {sha}",
        f"- Event: {event}",
        f"- Build & Test conclusion: {build}",
        f"- E2E conclusion: {e2e}",
        f"- Ollama model: {model}",
        "",
        "### Already true for this project (do NOT recommend adding these)",
        "- Syncfusion Blazor only (Interactive Server); no MudBlazor/Radzen/plain HTML grids",
        "- AuditLog append-only is already a Spec Kit / constitution requirement",
        "- Secrets via Keychain / user-secrets / env only (never recommend committing secrets)",
        "- Phase 1 complete; next product work is Phase 2 tenants (not generic refactors)",
        "- NAS fence: DS225+ ~6GB shared; 2 clerks; ≤~1.5GiB app; port 8082; linux/amd64; rare NAS deploys",
        "",
        "### TRX counters",
    ]

    failed_tests: list[str] = []
    any_failed = False
    saw_counters = False

    for path in sorted(glob.glob("ci-artifacts/**/*.trx", recursive=True)):
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError:
            lines.append(f"- {path}: (parse error)")
            continue

        counters = root.find(".//t:Counters", NS)
        if counters is None:
            counters = root.find(".//Counters")
        if counters is not None:
            saw_counters = True
            total = counters.attrib.get("total", "?")
            passed = counters.attrib.get("passed", "?")
            failed = counters.attrib.get("failed", "?")
            lines.append(
                f"- `{os.path.basename(path)}`: total={total} passed={passed} failed={failed}"
            )
            if str(failed) not in ("0", "?", ""):
                any_failed = True

        results = root.findall(".//t:UnitTestResult", NS)
        if not results:
            results = root.findall(".//UnitTestResult")
        for result in results:
            if result.attrib.get("outcome") != "Failed":
                continue
            any_failed = True
            name = (
                result.attrib.get("testName")
                or result.attrib.get("testId")
                or "unknown"
            )
            msg_el = result.find("t:Output/t:ErrorInfo/t:Message", NS)
            if msg_el is None:
                msg_el = result.find("Output/ErrorInfo/Message")
            msg = _text(msg_el)[:240]
            failed_tests.append(f"- FAIL `{name}`" + (f": {msg}" if msg else ""))

    if not saw_counters:
        lines.append("- (no TRX counters found)")

    lines.extend(["", "### Failed tests (evidence only)"])
    if failed_tests:
        lines.extend(failed_tests[:40])
    else:
        lines.append("- None")

    job_bad = build not in ("success", "skipped") or e2e not in ("success", "skipped")
    lines.extend(["", "### Job-level issues"])
    if job_bad and not any_failed:
        lines.append(
            f"- Job failed without parsed TRX failures (build={build}, e2e={e2e}). "
            "Treat as environmental/setup until logs say otherwise."
        )
    elif job_bad:
        lines.append(f"- Job conclusions: build={build}, e2e={e2e}")
    else:
        lines.append("- None (both jobs succeeded or skipped)")

    mode = "GREEN_RUN" if (not any_failed and not job_bad) else "FAILURE_RUN"
    lines.extend(
        [
            "",
            "### Review mode",
            f"- {mode}",
            "",
            "### Instructions for the model",
            "- Use ONLY evidence above. No repo-wide guesses.",
            "- Fluent Assertions / Xceed license text in TRX is NOT a failure.",
            "- On GREEN_RUN: do not invent coverage/refactor/audit/secrets work.",
        ]
    )

    summary = "\n".join(lines) + "\n"
    (out_dir / "ci-summary.md").write_text(summary, encoding="utf-8")
    (out_dir / "review-mode.txt").write_text(mode + "\n", encoding="utf-8")
    sys.stdout.write(summary)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
