# Conventions

Coding standards for this repo. They apply to human and AI-assisted changes alike.

## Data & EF Core
- Every entity extends `BaseModel` (`Id`, `CreatedAt`, `UpdatedAt`). Tenant-owned entities extend `TenantModel` (adds `ClubId`).
- Store enums as strings (`HasConversion<string>()`).
- Use plain LINQ (`.Contains()`, `.StartsWith()`), not `EF.Functions`.
- Raw SQL references actual table names, never `DbSet` property names.
- Migrations are created and applied manually by the maintainer; never at app startup or in CI.
- Never bypass tenant filters (`IgnoreQueryFilters()`) outside tenant resolution and platform-admin code.

## Architecture
- Business logic goes in services in `Infrastructure`. Controllers stay thin.
- Use view models instead of multiple `ViewBag` entries.
- Concrete services by default; no interface unless there are two real implementations.
- Look for an existing service before adding a new one for similar work.
- Avoid reflection when there is a reasonable alternative.

## Authorization
- Gate controllers and actions with `[Authorize]` / policies. Use `[AllowAnonymous]` for the few public actions.
- No manual `IsAuthenticated` checks and redirects.

## Front end
- Tailwind CSS for styling.
- No CDNs. Vendor third-party assets under `wwwroot/lib/` and reference them with `~/lib/...`.
- No `alert()` / `confirm()` / `prompt()`. Use the shared modal component.
- Consistent buttons: same styles, labels and placement for Edit, Delete and so on.
- Forms with per-field validation spans use `asp-validation-summary="ModelOnly"`, so no error shows twice.
- Index pages are paginated with DataTables 2.x: server-side, `type: 'POST'`, sorting by named fields, not column index.

## Integrations
- Config: an Options POCO in `Config/<Service>Options.cs`, bound from a root-level section, with SDK clients registered as lazily built singletons.
- HTTP: `IHttpClientFactory` with named clients; never `new HttpClient()`.
- Email: Razor views rendered to string by a reusable service; no HTML concatenation in C#.
- Files: Azure Blob Storage only (no `byte[]` in the database). Downloads redirect to short-lived, read-only SAS URLs with `rscd`/`rsct` overrides; containers stay private.
- CSV: CsvHelper, server-side. Never build CSV strings by hand.
- MIME types: use the MimeTypes package, not extension switches.

## Packages & build
- Pin exact NuGet versions; no wildcards.
- Builds must be warning-free. Treat warnings as work to do.

## Comments
- Write a comment only when the *why* isn't obvious, and keep it to one line.
- No banner comments, and no comments describing a change ("now handles…", "previously…"). That belongs in the commit message.
