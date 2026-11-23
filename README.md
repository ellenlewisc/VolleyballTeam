# Volleyball Team Manager

A multi-tenant web app for running a volleyball club: team rosters, game and practice schedules, drill-based practice plans, and updates for parents. Built with ASP.NET Core on .NET 10.

It started from a real problem: coordinating a youth club's teams, coaches and parents across group chats, spreadsheets and paper practice plans. Later, an **MCP server** will let an AI assistant build practice plans from the club's drill library.

> **Status:** Phase 0 (foundations) complete: sign-in, multi-tenancy, soft delete and auditing, tests, and push-to-deploy on Azure. Phase 1 (seasons, rosters, schedule) is next.

## Getting started

**Prerequisites:** .NET SDK 10.0.301+ (pinned in `global.json`), Node.js 24+ (builds Tailwind), and SQL Server LocalDB. Run `dotnet tool restore` once to get the pinned EF Core CLI.

```bash
# Create the local database (first run, and after any model change)
dotnet ef migrations add InitialCreate --project src/VolleyballTeam.Infrastructure --startup-project src/VolleyballTeam.Web --output-dir Data/Migrations
dotnet ef database update --project src/VolleyballTeam.Infrastructure --startup-project src/VolleyballTeam.Web

# Run the app (npm install and the Tailwind build run automatically on build)
dotnet run --project src/VolleyballTeam.Web
```

On startup, if the database has no clubs and the `Bootstrap` settings include an admin email and password, the app creates the first club and its owner. Locally these come from `appsettings.Development.json` (a demo club at `/demo-club`; sign in with that email and password). In production they are set once as App Service settings, then removed. See [DEPLOYMENT.md](docs/DEPLOYMENT.md).

```bash
dotnet test   # uses LocalDB; set VBT_TEST_SQLSERVER to point at another SQL Server
```

While editing views, run `npm run css:watch` in `src/VolleyballTeam.Web` for live Tailwind rebuilds.

## Tech stack

| Area | Choice |
|---|---|
| Runtime | .NET 10 (LTS) |
| Web | ASP.NET Core MVC, Razor views, Tailwind CSS |
| Data | EF Core 10, SQL Server / Azure SQL |
| Auth | ASP.NET Core Identity (invite-only) |
| Multi-tenancy | Shared database, `ClubId` discriminator, EF Core global query filters |
| Files / email | Azure Blob Storage (SAS downloads), Azure Communication Services Email |
| Hosting | Azure App Service (Free tier) + Azure SQL Database free offer |
| CI/CD | GitHub Actions |
| AI (planned) | MCP server using the official C# `ModelContextProtocol` SDK |

## Documentation

- [Project plan](docs/PROJECT_PLAN.md): goals, users, phases, risks, cost
- [Domain model](docs/DOMAIN.md): glossary, entities, roles and permissions
- [Architecture](docs/ARCHITECTURE.md): solution layout, how multi-tenancy works, request flow
- [Conventions](docs/CONVENTIONS.md): coding standards for this repo

### Architecture Decision Records

| # | Decision | Status |
|---|---|---|
| [0001](docs/adr/0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](docs/adr/0002-target-dotnet-10-lts.md) | Target .NET 10 LTS | Accepted |
| [0003](docs/adr/0003-razor-mvc-with-tailwind.md) | Razor MVC with Tailwind CSS | Accepted |
| [0004](docs/adr/0004-shared-database-multi-tenancy.md) | Shared-database multi-tenancy | Accepted |
| [0005](docs/adr/0005-club-as-tenant-team-scoped-roles.md) | Club as tenant, team-scoped roles | Accepted |
| [0006](docs/adr/0006-tenant-resolution-by-route-slug.md) | Resolve tenant from route slug | Accepted |
| [0007](docs/adr/0007-low-cost-azure-hosting.md) | Low-cost Azure hosting | Accepted |
| [0008](docs/adr/0008-aspnet-identity-invite-only.md) | ASP.NET Core Identity, invite-only | Accepted |
| [0009](docs/adr/0009-mcp-server-future-phase.md) | MCP server as a future phase | Proposed |
| [0010](docs/adr/0010-apply-migrations-in-deploy-pipeline.md) | Apply migrations in the deploy pipeline | Accepted |
| [0011](docs/adr/0011-soft-delete-and-deactivation.md) | Soft delete and user deactivation | Accepted |
| [0012](docs/adr/0012-seasons-and-team-history.md) | Seasons and team history | Accepted |
| [0013](docs/adr/0013-guardians-as-contacts.md) | Guardians as contacts, no parent logins in Phase 1 | Accepted |

New decisions use the [ADR template](docs/adr/0000-template.md).
