# Project Plan

## Vision

One place where a volleyball club runs its teams. Coaches manage rosters and plan practices from a shared drill library. Parents always know when and where the next game is, and get updates without hunting through group chats.

## Goals

1. **Be useful for a real club.** It replaces the spreadsheets, paper practice plans and scattered messages one club uses today.
2. **Show production-quality .NET.** That means multi-tenancy with tests that prove data isolation, clean layering, CI/CD, and documented decisions.
3. **Prepare for AI.** The data model and service layer are designed so an MCP server can be added later without a rewrite ([ADR 0009](adr/0009-mcp-server-future-phase.md)).
4. **Cost close to $0 to run** ([ADR 0007](adr/0007-low-cost-azure-hosting.md)).

## Non-goals (for now)

- Payments, club fees or registration.
- Native mobile apps. The web UI is responsive, and the iCal feed covers phone calendars.
- Detailed per-player statistics.
- Self-service sign-up for new clubs. Clubs are provisioned by an admin.

## Users

| Role | Needs |
|---|---|
| **Club Admin** | Sets club branding, creates teams, invites coaches |
| **Head / Assistant Coach** | Manages roster, schedule, practice plans, announcements for their team(s) |
| **Team Manager** | Helps with schedule, roster details and announcements |
| **Parent / Guardian** | Receives announcement emails (Phase 1); signs in to see their player's team and RSVP (Phase 3) |
| **Player** (later) | Read-only view of their team's schedule |

The full permissions matrix is in [DOMAIN.md](DOMAIN.md#roles-and-permissions).

## Phases

### Phase 0: Foundations
- Solution scaffold: Domain / Infrastructure / Web / Tests projects ([ARCHITECTURE.md](ARCHITECTURE.md))
- GitHub Actions: build, test, Tailwind build, deploy
- ASP.NET Core Identity with invite-only sign-up
- Tenancy plumbing: `TenantContext`, route-slug middleware, global query filters, `SaveChanges` interceptor
- Tenant isolation integration tests
- Deploy a minimal site to Azure free tier

**Done when:** a signed-in user can see `/{club}/` for their club only, isolation tests pass in CI, and the site is live on `*.azurewebsites.net`.

### Phase 1: MVP
Delivered in slices, each its own pull request and migration:

1. **Seasons and teams:** seasons (Upcoming, Active, Archived), teams that carry history, team seasons, staff, rollover, and history access rules ([ADR 0012](adr/0012-seasons-and-team-history.md))
2. **Players and roster:** club player directory, roster per team season, guardian contacts, roster CSV import and export (CsvHelper)
3. **Members, invitations and email:** member admin (roles, deactivation), staff invitations, password reset, Azure Communication Services email
4. **Schedule:** practices, games, tournaments, with location and opponent, weekly repeats and cancellations, in the club time zone (set in Club settings)
5. **Announcements:** posted to a team season and emailed to guardians and staff, with unsubscribe
6. **Club branding:** colors and logo (Blob Storage with SAS), added to Club settings
7. **Cleanup:** drop the Phase 0 team columns and table

Parents and players don't log in during Phase 1; guardians are email contacts ([ADR 0013](adr/0013-guardians-as-contacts.md)).

**Done when:** one real team's season (roster + schedule) is running in the app and its parents are receiving announcements.

### Phase 2: Practice planning
- Club-wide drill library with structured fields: skill focus, duration, player count, equipment, intensity
- Practice plans built from ordered drills, with a running total time
- Attach a plan to a practice event
- Printable plan view for the gym

**Done when:** a coach can build and print a 90-minute practice from library drills in under five minutes.

### Phase 3: Engagement
- Parent logins (guardian contacts linked to accounts by email)
- RSVPs and attendance per event
- Game results (set-by-set scores)
- Subscribable iCal feed per team, so parents can add the schedule to their phone calendar

**Done when:** parents subscribe to the team calendar and coaches see who is attending each event.

### Phase 4: MCP server
- Expose drills, roster and schedule to AI assistants through the official C# MCP SDK
- `draft_practice_plan` tool creates a **Draft** plan that a coach reviews and publishes
- Tool calls are limited to the caller's club and teams

**Done when:** an AI assistant can draft a practice plan from the club's own drills, and the coach approves it in the web app. See [ADR 0009](adr/0009-mcp-server-future-phase.md).

### Later / maybe
Self-service club sign-up, SMS notifications, player stats, tournament brackets.

## Risks

| Risk | Mitigation |
|---|---|
| Free-tier limits (F1 CPU quota, SQL auto-pause cold starts) | Acceptable for one club; documented upgrade path ([ADR 0007](adr/0007-low-cost-azure-hosting.md)) |
| Data leaking between tenants | Global query filters, write-side interceptor, and isolation tests in CI |
| **Minors' data privacy** | Store minimal player data (no birthdates, addresses or photos); parents see only their own team; invite-only access |
| Scope creep | Phases have "done when" criteria; non-goals are listed explicitly |
| Free-tier terms change | Re-check pricing when provisioning; the app does not depend on any tier-specific feature |

## Estimated running cost

About **$0–2/month**. The breakdown is in [ADR 0007](adr/0007-low-cost-azure-hosting.md).
