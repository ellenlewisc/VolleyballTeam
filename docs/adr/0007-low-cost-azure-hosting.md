# 0007. Low-cost Azure hosting

- **Status:** Accepted
- **Date:** 2026-11-20

## Context

This is a personal project serving one club, so running costs must be close to $0. The maintainer already works with Azure App Service, Azure SQL, Blob Storage and EF Core with SQL Server, and wants to keep that workflow.

## Decision

| Piece | Service | Expected cost |
|---|---|---|
| Web app | Azure App Service **F1 (Free)**, Linux, .NET 10 | $0 |
| Database | **Azure SQL Database free offer** (serverless, auto-pause) | $0 |
| Files | Azure Blob Storage, private container, SAS downloads | cents/month |
| Email | Azure Communication Services Email | ~$0 at club volume |
| CI/CD | GitHub Actions (public repo) | $0 |

Local development uses SQL Server LocalDB.

## Consequences

- **Cold starts:** F1 has no "Always On", and the free SQL database auto-pauses when idle. The first request after a quiet period can take up to a minute. That's acceptable for a club app.
- **F1 limits:** a daily CPU-minute quota, 1 GB RAM, no deployment slots, no custom-domain TLS. The app runs at `*.azurewebsites.net`.
- **Upgrade path:** move to App Service B1 (about $13/month) for Always On and a custom domain, with no code change.
- **Fallback:** if F1 limits are hit, Azure Container Apps on the consumption plan (scales to zero, monthly free grant) runs the same app in a container.
- Free-tier terms change, so re-check them when provisioning.

## Alternatives considered

- **App Service B1 + standard Azure SQL:** the familiar setup, but it costs more than this project justifies.
- **PostgreSQL (Neon, Supabase free tiers):** cheap, but adds a different provider and database dialect for no gain.
- **SQLite on App Service:** cheapest, but file locking and app-restart persistence make it fragile on App Service.
- **Non-Azure PaaS (Render, Railway, Fly.io):** viable, but moves away from the maintainer's Azure experience and the Blob/ACS integrations.
