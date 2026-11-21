# Architecture

## Solution layout

```
VolleyballTeam.slnx
src/
  VolleyballTeam.Domain/          Entities, enums, BaseModel / TenantModel. Depends only on
                                  Microsoft.Extensions.Identity.Stores (for AppUser).
  VolleyballTeam.Infrastructure/  AppDbContext, EF configurations, migrations, TenantContext,
                                  services (Roster, Schedule, PracticePlan, Announcement),
                                  BlobStorageService, EmailService
  VolleyballTeam.Web/             ASP.NET Core MVC: controllers, view models, Razor views, Tailwind
  VolleyballTeam.Mcp/             (Phase 4) MCP tools hosted in Web; reuse Infrastructure services
tests/
  VolleyballTeam.Tests/           xUnit integration tests (tenant isolation, services, endpoints)
```

Business logic lives in **services in Infrastructure**, not in controllers. Controllers translate between HTTP and view models. This lets the future MCP tools call the same services, with the same tenant rules, as the web UI.

Services are concrete classes registered directly in DI. Interfaces are added only where there are genuinely two implementations ([CONVENTIONS.md](CONVENTIONS.md)).

## Multi-tenancy

Shared database, one `ClubId` column per tenant-owned table ([ADR 0004](adr/0004-shared-database-multi-tenancy.md)). The tenant comes from the URL ([ADR 0006](adr/0006-tenant-resolution-by-route-slug.md)).

### Request flow

```mermaid
sequenceDiagram
    participant B as Browser
    participant M as TenantMiddleware
    participant A as Authorization
    participant C as Controller
    participant S as Service
    participant D as AppDbContext

    B->>M: GET /lakeside-vbc/teams/12/roster
    M->>D: Club by slug "lakeside-vbc"
    M->>M: TenantContext.ClubId = 3 (404 if unknown)
    M->>A: continue
    A->>A: [Authorize] + ClubMembership for club 3?
    A->>C: TeamAccess handler: TeamRole on team 12?
    C->>S: RosterService.GetRoster(12)
    S->>D: query Players (global filter: ClubId == 3)
    D-->>B: roster view
```

### Enforcement layers

1. **Read side:** a named EF Core query filter (`QueryFilters.Tenant`, `e => e.ClubId == CurrentClubId`) is applied to every `TenantModel` type in `OnModelCreating`. A forgotten `Where` can't leak rows. A second named filter hides soft-deleted rows ([ADR 0011](adr/0011-soft-delete-and-deactivation.md)), and code can ignore that one without dropping the tenant filter.
2. **Write side:** a `SaveChangesInterceptor` sets `ClubId` on added entities and throws if a modified or added entity's `ClubId` doesn't match the current tenant.
3. **Database keys:** child rows reference their parent by `(ClubId, ParentId)` against an alternate key on the parent. For example, a `TeamMembership` can only point at a team in its own club, and SQL Server itself rejects anything else.
4. **Authorization:** `[Authorize]` on controllers, a club-membership requirement, and resource-based handlers for team access using `TeamRole`.
5. **Tests:** seed two clubs, then assert through services *and* HTTP endpoints that club A never sees, edits or deletes club B's data. These run in CI on every push.

Ignoring `QueryFilters.Tenant` is allowed only in tenant resolution and platform-admin code, and every use is covered by a test.

Club slugs are lowercase and URL-safe, and top-level route names (`account`, `error`, `admin`…) are reserved, so a club can never shadow an app route (`ClubSlug` and the `clubslug` route constraint).

EF Core's SQL Server retry strategy is enabled, so requests survive the free-tier database resuming from auto-pause.

## Security

| Concern | How |
|---|---|
| Sign-in | ASP.NET Core Identity: confirmed email required, 10+ character passwords, lockout for 15 minutes after 5 failures, and a per-IP rate limit (10 attempts a minute) on the login form. |
| Inactive people | `AppSignInManager` refuses sign-in for deactivated accounts and rejects their existing cookies at the next security-stamp check (every 5 minutes). The club-membership handler checks account and membership status on every request. |
| Cookies | `HttpOnly`, `SameSite=Lax`, HTTPS-only outside Development; 14-day sliding expiry. |
| CSRF | Antiforgery validation on every unsafe request through a global `AutoValidateAntiforgeryToken` filter. |
| Headers | `Content-Security-Policy` (scripts from this site only, no framing), `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, HSTS outside Development, and `Cache-Control: no-store` on signed-in pages. |
| Proxy | Forwarded headers trust one hop, the App Service front end, so client IPs and HTTPS are seen correctly. |

## Front end

- Razor views styled with **Tailwind CSS**, built with the Tailwind standalone CLI at build time and in CI. No CDN.
- Third-party JS/CSS (DataTables 2.x and others) is vendored under `wwwroot/lib/`.
- Index pages use DataTables with **server-side processing over POST** and sorting by named fields.
- Club branding: the layout sets CSS custom properties (`--club-primary`, `--club-secondary`) from the current `Club`, and Tailwind utilities reference them.
- A shared confirmation modal replaces browser `alert`/`confirm`.

## Cross-cutting services

| Service | Responsibility |
|---|---|
| `TenantContext` | Scoped; holds current `ClubId`, slug, time zone, branding |
| `BlobStorageService` | Uploads to a private container; mints 15-minute read-only SAS URLs (with `rscd`/`rsct`) for redirects |
| `EmailService` | Renders Razor email views to strings and sends via Azure Communication Services |
| `RazorViewRenderer` | Reusable view-to-string rendering for emails |

Config follows the Options pattern: `Config/<Service>Options.cs` bound from a root-level `appsettings.json` section, with SDK clients registered as singletons that are built lazily.

## Testing

- **xUnit** integration tests against a real SQL Server (LocalDB locally; a SQL Server container in CI), so query filters and string-enum conversions behave as they will in production.
- `WebApplicationFactory` for endpoint-level authorization and isolation tests.

## Deployment

Push to `main` → **CI** (Tailwind build → build → test against SQL Server) → **Deploy** (publish → build an EF Core migration bundle → apply migrations to Azure SQL → deploy to App Service). A failed test or migration stops the deploy. Migrations are written by the developer and applied by the pipeline, never at app startup. See [ADR 0007](adr/0007-low-cost-azure-hosting.md), [ADR 0010](adr/0010-apply-migrations-in-deploy-pipeline.md) and [DEPLOYMENT.md](DEPLOYMENT.md).
