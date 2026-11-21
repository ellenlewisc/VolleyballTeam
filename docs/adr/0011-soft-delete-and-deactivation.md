# 0011. Soft delete and user deactivation

- **Status:** Accepted
- **Date:** 2026-11-20

## Context

The club keeps team history permanently: past rosters, coaches and seasons. A hard `DELETE` would silently remove that history or fail on foreign keys. People also come and go: a coach leaves the club, a parent's child ages out. Their access must end, but their name must stay on old rosters.

## Decision

**Soft delete for every entity.** `BaseModel` has a nullable `DeletedAt`. A `SoftDeleteInterceptor` turns every `Remove()` of a `BaseModel` into an update that stamps `DeletedAt`, so application code can't hard-delete by accident. A named EF Core query filter (`QueryFilters.SoftDelete`) hides deleted rows, separate from the tenant filter (`QueryFilters.Tenant`). Code that needs deleted rows ignores only the soft-delete filter, so the tenant filter always stays on. A test fails if any entity is missing either filter.

Unique indexes are filtered on `[DeletedAt] IS NULL`, so a removed membership or team name can be reused. Club slugs are the exception: they stay unique forever, so an old link never points at a different club.

**Who changed it.** `BaseModel` also records `CreatedById`, `UpdatedById` and `DeletedById` (foreign keys to the user). An `AuditInterceptor` stamps them, with the timestamps, from a scoped `CurrentUser` that middleware fills from the signed-in user. `CreatedAt` and `CreatedById` can't be overwritten by an edit. They are null for system changes such as bootstrap.

**Deactivation for people, at two levels:**

| Level | Field | Effect |
|---|---|---|
| Account | `AppUser.DeactivatedAt` | Can't sign in to any club. Existing sessions end within 5 minutes (security-stamp revalidation), and club pages refuse them at once. |
| Club | `ClubMembership.DeactivatedAt` | Loses access to that club only. Other clubs are unaffected. |

Deactivation is reversible and keeps the person on historical records. User accounts are never soft-deleted or hard-deleted by the app.

## Consequences

- History survives every delete, and the deleted-or-not state of a row is visible in the database.
- Queries that `Include` a soft-deleted required parent drop the child row too. This is usually right: a deleted team hides its memberships. Where history must show deleted parents, ignore `QueryFilters.SoftDelete` on that query.
- Rows accumulate. At one club's volume this is not a concern.
- A real erase (for example a parent's privacy request for a minor) will need a deliberate admin process that bypasses the interceptor. It is out of scope until needed.

## Alternatives considered

- **Hard delete with cascades:** simplest, but loses history, which the club explicitly wants kept.
- **Per-entity `IsDeleted` flags handled in each service:** easy to forget in one query; the interceptor and filters make it the default.
- **Temporal tables:** full row history for free, but more storage and complexity than needed, and they don't hide deleted rows from normal queries.
