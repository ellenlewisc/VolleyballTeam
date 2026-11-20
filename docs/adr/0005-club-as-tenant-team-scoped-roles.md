# 0005. Club as tenant, team-scoped roles

- **Status:** Accepted
- **Date:** 2026-11-20

## Context

Volleyball clubs run several teams (e.g. 14U, 16U, 18U) under one brand. The club owns the identity (name, logo, colors), but the teams operate almost independently: different coaches, rosters and schedules. A parent usually cares about one team; a coach might run two.

## Decision

The **tenant is the Club.** Branding, the drill library and membership belong to the club. Inside a club, access is **scoped per team**:

- `ClubMembership` grants entry to the club with a `ClubRole` (Owner / Admin / Member). Owners and Admins manage everything in the club.
- `TeamMembership` grants a `TeamRole` (HeadCoach / AssistantCoach / TeamManager / Parent) on a specific team. Team data is visible only to members of that team (and club admins).
- The drill library is **club-wide** so coaches can share drills. Everything else (roster, schedule, plans, announcements) is team-owned.

## Consequences

- Club branding and a shared drill library come for free; a team-level tenant couldn't share either.
- Two levels of authorization (club, then team) are more complex than one, and are handled with resource-based authorization handlers.
- Supports a single club today and many clubs later without remodeling.

## Alternatives considered

- **Tenant = Team:** simpler, but branding and drills would be duplicated per team and coaches with two teams would need two accounts' worth of access.
- **Club-wide roles only:** parents would see every team's roster, which is unacceptable for minors' data.
