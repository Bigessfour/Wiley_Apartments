# ClerkSuite tests

Three-layer test pyramid aligned with spec-kit:

| Project                             | Layer           | Scope                                                                |
| ----------------------------------- | --------------- | -------------------------------------------------------------------- |
| `Wiley.Apartments.Tests`            | **Unit**        | Pure logic: `DateTimeService`, `AuditLogAppender`, config resolution |
| `Wiley.Apartments.IntegrationTests` | **Integration** | `WebApplicationFactory`, Identity seed, audit interceptor, auth HTTP |
| `Wiley.Apartments.E2ETests`         | **E2E**         | Playwright browser flows against running test host                   |

## Run all tests

```bash
dotnet test Wiley.Apartments.slnx
```

## E2E prerequisites (first time)

```bash
dotnet build tests/Wiley.Apartments.E2ETests
pwsh tests/Wiley.Apartments.E2ETests/bin/Debug/net9.0/playwright.ps1 install chromium
```

## Coverage policy

Each new service, interceptor, or infrastructure helper **must** ship with:

1. Unit tests (when logic is isolated)
2. Integration test (when touching EF, Identity, or HTTP pipeline)
3. E2E test (when affecting clerk-visible routes)

Add tests in the same PR as the feature (Phase 1+).
