# 0002. Target .NET 10 LTS

- **Status:** Accepted
- **Date:** 2026-11-20

## Context

The project starts in November 2026. .NET 9 is a Standard Term Support release, and its support ended on 10 November 2026. .NET 10, released in November 2025, is a Long Term Support release supported until November 2028. A new project should not start on an unsupported runtime.

## Decision

We will target **.NET 10** (`net10.0`) with EF Core 10 and ASP.NET Core 10. `global.json` pins SDK `10.0.301` with `rollForward: latestFeature`, so patch and feature-band updates are picked up without changing major versions.

## Consequences

- Security patches until November 2028, with time to plan the move to .NET 12 (the next LTS).
- Shows current .NET (C# 14 features, `.slnx` solution format, latest EF Core).
- Azure App Service supports .NET 10 on Linux.

## Alternatives considered

- **.NET 9:** out of support.
- **Wait for .NET 11 (STS, Nov 2026):** shorter support window, and not needed for anything in scope.
