# 0003. Razor MVC with Tailwind CSS

- **Status:** Accepted
- **Date:** 2026-11-20

## Context

The app is mostly forms and lists (rosters, schedules, plans) used by coaches and parents, often on phones. It is built by one developer whose strongest skills are C# and ASP.NET Core. The goal is to ship useful features quickly and keep the C# side front and center.

## Decision

We will build server-rendered **ASP.NET Core MVC with Razor views**, styled with **Tailwind CSS** (standalone CLI, no Node toolchain required). Interactive pieces use small amounts of vanilla JS and vendored libraries (e.g. DataTables 2.x).

## Consequences

- One deployable app, one language for most of the code, fast iteration.
- No separate API to version for the UI. The MCP server (Phase 4) will call services directly instead.
- Less flashy front-end showcase than a SPA. If a rich client is ever needed, the service layer can sit behind an API.

## Alternatives considered

- **React SPA + Web API:** a stronger front-end showcase, but roughly double the surface area (API contracts, auth for the SPA, a second build pipeline) for a forms-heavy app.
- **Blazor:** all-.NET, but less common in the job market than MVC or React, and Blazor Server's persistent connections don't suit free-tier hosting.
