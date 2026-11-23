# 0012. Seasons and team history

- **Status:** Accepted
- **Date:** 2026-11-23

## Context

Phase 0 modelled a team as one row per season ("16U Black, 2026–27"). The club wants more than that:
- carry a team into the next year,
- reuse a team name for an all-new group of kids,
- move a group up an age group (16U → 17U),
- keep every past season forever.

History must also be visible only to the right people. A coach who has moved on shouldn't see the live team they left.

## Decision

Split "team" into three entities:

| Entity | Meaning |
|---|---|
| `Season` | A club-wide year (e.g. 2026-27) with a status: **Upcoming**, **Active** or **Archived** |
| `Team` | The stable identity that carries history (e.g. "Black") |
| `TeamSeason` | That team in one season: display name, age group, level, staff, and later roster, schedule and announcements |

- Staff are assigned per TeamSeason (`TeamSeasonStaff`: HeadCoach, AssistantCoach, TeamManager).
- A **rollover** creates the next TeamSeason from the previous one and records `RolledOverFromId`. The admin can rename it, change the age group or level, and tick which staff come along.
- An all-new group under an existing name is simply the same Team with a fresh TeamSeason.

**Admins control the season lifecycle.** An admin starts a season and archives it; next season can be set up while the current one runs.

**Archived seasons are read-only for everyone**, admins included. An admin can reopen one to fix a mistake.

Access is decided per TeamSeason by one function, `TeamSeasonAccess.Decide`:

| Who | Upcoming / Active season | Archived season |
|---|---|---|
| Club Owner / Admin | Edit | Read-only |
| Head coach of that TeamSeason | Edit | Read-only (automatic) |
| Assistant coach / team manager of that TeamSeason | Edit | None |
| Anyone else, or a deactivated member | None | None |

A former head coach therefore sees the seasons they coached but not the team's current season, which belongs to its new staff.

## Consequences

- History is complete and cheap: past TeamSeasons are rows that never change.
- Every team-scoped feature (roster, schedule, announcements) hangs off `TeamSeason`, so it inherits these access rules.
- The Phase 0 schema is migrated in two releases (ADR 0010):
  - **This release:** add the new tables and backfill them from `Teams.Season` and `TeamMemberships`. Those old columns and that table become unused.
  - **A later cleanup migration:** drop them.

## Alternatives considered

- **One team row per season with a "previous team" link:** simpler tables, but there's no stable identity to group a team's history, and renaming breaks the chain.
- **Date-driven season status:** no admin action needed, but no control around tryouts and overlapping seasons.
- **Explicit history grants by an admin:** more control, but more admin work for the common case; head coaches seeing their own past seasons is the expected default.
