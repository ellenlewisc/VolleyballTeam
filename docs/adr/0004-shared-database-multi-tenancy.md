# 0004. Shared-database multi-tenancy

- **Status:** Accepted
- **Date:** 2026-11-20

## Context

The app must support multiple clubs (tenants) whose data never mixes, even though only one club will use it at first. Hosting must be close to free ([ADR 0007](0007-low-cost-azure-hosting.md)), and schema changes should stay simple for a single maintainer.

## Decision

We will use **one shared database** where every tenant-owned table has a `ClubId` column (via the `TenantModel` base class). Isolation is enforced in layers:

1. EF Core **global query filters** on every `TenantModel`, registered in a single loop in `OnModelCreating`.
2. A **`SaveChangesInterceptor`** that stamps `ClubId` on inserts and rejects cross-tenant writes.
3. **Authorization** on club membership and team role.
4. **Integration tests** that seed two clubs and prove neither can read or change the other's data.

`ClubId` is denormalized onto child tables (e.g. `PracticePlanItem`) so each table can be filtered on its own, and indexed as the leading column of composite indexes.

## Consequences

- One database, one migration path, near-zero hosting cost.
- Isolation depends on the app being correct, which is why the interceptor and tests are not optional.
- Moving a large tenant to its own database later means migrating its data, but that is possible because every row is tagged.

## Alternatives considered

- **Database per tenant:** strongest isolation, but each club needs its own database (cost), provisioning, and migrations run N times. Too much for a personal-scale app.
- **Schema per tenant:** awkward in EF Core (model caching per schema) and still N-times migrations.
- **SQL Server row-level security:** a good extra layer later, but harder to test and to understand than EF filters for now.
