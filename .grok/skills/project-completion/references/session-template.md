# Surface session template

Copy into `docs/handover/surface-logs/YYYY-MM-DD-<id>.md` or a PR comment.

```markdown
## Surface: <ID> <route>
**Date:**
**Reviewer:** builder / operator A / operator B
**Environment:** local | staging | production URL
**Build / image / commit:**

### 1. Arrive
- [ ] Nav or deep link reaches page (no 404 / auth loop)
- [ ] Title + subtitle make the job obvious
- [ ] Primary action visible without scrolling on laptop

### 2. Format
- [ ] Controls match app kit and neighboring pages
- [ ] Theme contrast OK (light + dark if offered)
- [ ] No horizontal overflow at ~1280px and ~390px
- [ ] Loading and empty states are real (not blank white)
- [ ] Errors show toast/banner with recovery

### 3. Connected
- [ ] Reads/writes correct service (not mock/stale)
- [ ] Create / edit / soft-delete persists and reloads
- [ ] Rows that should open detail include id in URL
- [ ] Cross-links: parent ↔ child ↔ money ↔ queue ↔ docs
- [ ] Domain rules held (facility exclusion, deposits ≠ rent, etc.)

### 4. Usable — job story
> “As <role>, I need to ____ so that ____.”

- [ ] Happy path completes (note click count)
- [ ] Validation is plain language
- [ ] Destructive actions confirm
- [ ] Print/export if the job needs a paper trail

### 5. Bugs
| Sev | Issue | Repro | Fix |

### 6. Verdict
- [ ] PASS — D2
- [ ] PASS WITH NOTES (non-blocking S2/S3 only)
- [ ] FAIL (open S0/S1)

**Training notes:**
```

## Cross-cutting checks (after core path)

| Check | How |
|-------|-----|
| Dashboard → list → detail | Follow each KPI/list row |
| Write → aggregate update | Create money/queue item; refresh dashboard/report |
| Soft-delete safety | Hidden from default lists; no crash |
| Domain exclusions | e.g. facility out of occupancy counts |

## Severity quick ref

- **S0** money/security/data loss  
- **S1** job blocked  
- **S2** workaround exists  
- **S3** polish  
