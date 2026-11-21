# 0009. MCP server as a future phase

- **Status:** Proposed
- **Date:** 2026-11-20

## Context

A key goal is letting an AI assistant help coaches, for example: "Build me a 90-minute practice focused on serve receive for 10 players." The Model Context Protocol (MCP) is the standard way to expose tools and data to AI assistants, and there is an official C# SDK (`ModelContextProtocol`, plus `ModelContextProtocol.AspNetCore` for HTTP hosting). Building it now would delay the core app, but early design choices could block it later.

## Decision

The MCP server is **Phase 4**. Until then, the core app must keep these constraints so MCP can be added without a rewrite:

1. **Logic in services, not controllers.** MCP tools call the same `Infrastructure` services, with the same tenant filters, as the web UI.
2. **Structured drill data.** Drills have typed fields (`SkillFocus`, duration, player range, equipment, `Intensity`), not just free text, so an AI can pick and sequence them.
3. **Draft status on practice plans.** AI-created plans are saved as `Draft` and only a coach can publish them, so a person stays in the loop.
4. **Tenant context outside HTTP routing.** `TenantContext` can be set from an authenticated token, not only from the route slug.

Planned shape (to be confirmed when Phase 4 starts):

- Hosted over HTTP in the existing App Service (no extra cost) using `ModelContextProtocol.AspNetCore`.
- Candidate tools: `search_drills`, `get_team_roster`, `list_upcoming_events`, `draft_practice_plan`.
- Auth: OAuth vs. per-user, club-scoped API tokens, decided at the start of Phase 4.

## Consequences

- Phase 1–3 work stays focused on the core app.
- The four constraints add a little up-front discipline but no extra features.
- This ADR will be revised to **Accepted** (or superseded) with the final transport and auth choice.

## Alternatives considered

- **Build MCP first:** an impressive demo, but there would be no real data or users behind it.
- **Separate MCP service with its own database access:** duplicates tenant enforcement and adds hosting cost.
