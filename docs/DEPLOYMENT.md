# Deployment

Production runs on the free Azure tiers chosen in [ADR 0007](adr/0007-low-cost-azure-hosting.md). This guide sets it up from an empty subscription. Use the **same region** for every resource.

> **Secrets never go in the repo.** The connection string, bootstrap password and publish profile live in App Service settings and GitHub secrets only.

## 1. Resource group

Portal → **Resource groups** → **Create**: name `rg-volleyball`, pick your region.

## 2. Azure SQL Database (free offer)

Portal → **Azure SQL** → **SQL databases** → **Create**.

1. Click **Apply offer** on the free-offer banner at the top. If it isn't shown, the subscription has already used its free databases.
2. **Basics:** resource group `rg-volleyball`. Database name `volleyball`. **Server → Create new**: a globally unique name (e.g. `sql-volleyball-<something>`), same region, authentication **Use SQL authentication**, with an admin login and a strong password you store in a password manager.
3. **Free database offer:** when the free limit is reached → **Auto-pause the database until next month**. Never choose "continue using for additional charges".
4. **Networking:** connectivity **Public endpoint**. **Allow Azure services and resources to access this server → Yes**. **Add current client IP address → Yes**, so you can run migrations from your PC.
5. **Review + create.**

Then open the database → **Connection strings** → copy the **ADO.NET (SQL authentication)** string and replace `{your_password}`.

## 3. Create the schema (optional)

The Deploy workflow applies migrations automatically (step 7). To create the schema right away from your PC instead, run this from the repo root. Use **single quotes**, so characters like `$` or `!` in the password aren't changed by the shell, and never save the string to a file:

```bash
dotnet ef database update --project src/VolleyballTeam.Infrastructure --startup-project src/VolleyballTeam.Web --connection '<ADO.NET connection string>'
```

## 4. App Service (F1 Free)

Portal → **App Services** → **Create** → **Web App**.

1. **Basics:** resource group `rg-volleyball`, a globally unique name (it becomes `<name>.azurewebsites.net`), **Publish: Code**, **Runtime stack: .NET 10 (LTS)**, **OS: Linux**, same region.
2. **Pricing plan:** create a new Linux plan and choose **Free F1**. Use *Explore pricing plans* if it defaults to a paid tier.
3. **Deployment:** leave continuous deployment **disabled**; this repo has its own workflow.
4. **Monitoring:** Application Insights **No** (it can be added later).
5. **Review + create.**

## 5. Configure the app

Open the web app:

1. **Settings → Environment variables → Connection strings → Add:** name `DefaultConnection`, value the ADO.NET string, type **SQLAzure**.
2. **Settings → Environment variables → App settings → Add** the one-time bootstrap values (the first club and your owner login):

   | Name | Value |
   |---|---|
   | `Bootstrap__ClubName` | your club's name |
   | `Bootstrap__ClubSlug` | URL name, lowercase with dashes (e.g. `lakeside-vbc`) |
   | `Bootstrap__TimeZoneId` | e.g. `America/Toronto` |
   | `Bootstrap__AdminEmail` | your login email |
   | `Bootstrap__AdminPassword` | a strong password (10+ chars, upper, lower, digit, symbol) |
   | `Bootstrap__AdminFirstName` / `Bootstrap__AdminLastName` | your name |
   | `Bootstrap__TeamName` / `Bootstrap__TeamSeason` | optional first team, e.g. `16U` / `2026-27` |

3. **Settings → Configuration → General settings:** **SCM Basic Auth Publishing Credentials → On** (required for publish-profile deploys). Under **Settings → Configuration**, also confirm **HTTPS Only → On**.
4. **Apply / Save** (the app restarts).

## 6. Connect GitHub

1. Web app **Overview → Download publish profile**.
2. GitHub repo → **Settings → Secrets and variables → Actions**:
   - **Secrets → New repository secret:** `AZURE_WEBAPP_PUBLISH_PROFILE` = the full contents of the downloaded file. Delete the file afterwards.
   - **Secrets → New repository secret:** `AZURE_SQL_CONNECTION_STRING` = the ADO.NET connection string with the real password (the same value as `DefaultConnection`).
   - **Variables → New repository variable:** `AZURE_WEBAPP_NAME` = the web app name.

## 7. Deploy

Merge to `main`. CI runs, and when it passes, the **Deploy** workflow:

1. publishes the app,
2. builds an EF Core migration bundle and applies any pending migrations to Azure SQL ([ADR 0010](adr/0010-apply-migrations-in-deploy-pipeline.md)),
3. deploys to App Service.

If the migration step fails, nothing is deployed. The workflow can also be run manually from **Actions → Deploy → Run workflow**. If the migration step reports that the client IP isn't allowed, confirm **Allow Azure services and resources to access this server** is ticked on the SQL server's **Networking** page.

The first start creates your club and owner account from the bootstrap settings. Browse to `https://<name>.azurewebsites.net`, sign in, and confirm you land on `/<your-slug>`.

## 8. Remove the bootstrap password

Once you can sign in, delete `Bootstrap__AdminPassword` (and the other `Bootstrap__*` settings if you like) from **App settings** and save. Bootstrap never runs again once a club exists, but the password shouldn't sit in config.

## Expected behaviour on the free tier

- The first request after a quiet period can take 30–60 seconds while the app and database wake up. The database connection retries automatically.
- F1 allows 60 CPU-minutes a day, which is plenty for one club.
- Monthly cost should stay at $0. The $5 budget alert is the safety net.

## Future migrations

Create the migration locally (`dotnet ef migrations add <Name> ...`), test it against LocalDB, and commit it with the model change. Pushing to `main` applies it to production before the new code goes live. Keep migrations backward compatible: add first, drop or rename in a later release. The app never migrates itself at startup.
