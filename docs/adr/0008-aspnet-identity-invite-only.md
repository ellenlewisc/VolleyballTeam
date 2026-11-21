# 0008. ASP.NET Core Identity, invite-only

- **Status:** Accepted
- **Date:** 2026-11-20

## Context

Users are club admins, coaches and parents of minors. Nobody should be able to sign up and browse a team; access should come from someone who already belongs to the club or team. Cost must stay at zero, and the identity data must live alongside tenant data.

## Decision

We will use **ASP.NET Core Identity** (cookie auth, `IdentityUser<int>`) stored in the app database. Registration is **invite-only**:

- Club admins invite coaches; coaches and team managers invite parents.
- An invitation is a single-use, expiring token, emailed through the `EmailService`. Accepting it creates the account (or links an existing one) and the corresponding `ClubMembership` / `TeamMembership`.
- Email confirmation is required and lockout is enabled. External logins (e.g. Google) can be added later.

## Consequences

- No external identity provider cost or dependency; users live in the same database as memberships.
- We own password storage and reset flows, but Identity provides them.
- The MCP server (Phase 4) will need a token-based scheme in addition to cookies ([ADR 0009](0009-mcp-server-future-phase.md)).

## Alternatives considered

- **Microsoft Entra External ID:** strong option, but more setup and another service to manage for a one-club app.
- **Open registration with approval:** exposes a sign-up surface and puts approval work on coaches.
