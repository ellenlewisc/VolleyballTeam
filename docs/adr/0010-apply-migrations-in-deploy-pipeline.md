# 0010. Apply migrations in the deploy pipeline

- **Status:** Accepted
- **Date:** 2026-11-20

## Context

Deployment is push-to-deploy: a merge to `main` runs CI and then publishes to Azure App Service. Applying schema changes by hand from a developer machine is easy to forget, which leaves new code running against an old schema. It also needs the developer's IP in the SQL firewall and the production connection string in a local terminal.

## Decision

The Deploy workflow builds an **EF Core migration bundle** (`dotnet ef migrations bundle`, tool version pinned in `dotnet-tools.json`) and runs it against Azure SQL **before** deploying the app. The connection string comes from the `AZURE_SQL_CONNECTION_STRING` GitHub secret. If the migration fails, the deploy stops and the previous version keeps running.

Migrations are still **written** by the developer and reviewed in the pull request. The app never migrates itself at startup.

## Consequences

- Push to `main` updates code and schema together, with nothing to remember.
- For a short window, the new schema runs with the old code, so migrations must be backward compatible (add columns and tables first; drop or rename in a later release).
- A bad migration reaches production without a manual check. That's acceptable for a single-club app with tested migrations and Azure SQL point-in-time restore available.
- The pipeline's runner must reach Azure SQL. GitHub-hosted runners run on Azure, so the server's "Allow Azure services" exception covers them.

## Alternatives considered

- **Manual `dotnet ef database update`:** simplest, but easy to forget, and it couples deploys to one machine.
- **`Database.Migrate()` at app startup:** no extra pipeline step, but a failure takes the site down at boot, races if several instances start, and needs DDL rights for the app's own login.
- **Generated SQL scripts run by a separate tool:** most control, but more moving parts than this project needs.
