using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Config;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Infrastructure.Seeding;

public partial class DevDataSeeder(
    AppDbContext db,
    TenantContext tenant,
    UserManager<AppUser> users,
    IOptions<DevSeedOptions> options,
    ILogger<DevDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.AdminEmail) || string.IsNullOrWhiteSpace(settings.AdminPassword))
        {
            LogSkipped(logger, "DevSeed:AdminEmail and DevSeed:AdminPassword are not configured");
            return;
        }

        if (!db.Database.GetMigrations().Any() || !await db.Database.CanConnectAsync(cancellationToken))
        {
            LogSkipped(logger, "the database has no migrations or is unreachable");
            return;
        }

        if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            LogSkipped(logger, "there are pending migrations; run dotnet ef database update");
            return;
        }

        if (await db.Clubs.AnyAsync(cancellationToken))
        {
            return;
        }

        var club = new Club { Name = settings.ClubName, Slug = settings.ClubSlug, TimeZoneId = settings.TimeZoneId };
        db.Clubs.Add(club);
        await db.SaveChangesAsync(cancellationToken);
        tenant.SetClub(club);

        var owner = new AppUser
        {
            UserName = settings.AdminEmail,
            Email = settings.AdminEmail,
            EmailConfirmed = true,
            FirstName = settings.AdminFirstName,
            LastName = settings.AdminLastName,
        };

        var result = await users.CreateAsync(owner, settings.AdminPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Dev seed user could not be created: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        var team = new Team { Name = "16U", Season = "2026-27", AgeGroup = "16U", Level = "Club" };
        db.ClubMemberships.Add(new ClubMembership { UserId = owner.Id, Role = ClubRole.Owner });
        db.Teams.Add(team);
        db.TeamMemberships.Add(new TeamMembership { UserId = owner.Id, Team = team, Role = TeamRole.HeadCoach });
        await db.SaveChangesAsync(cancellationToken);

        LogSeeded(logger, club.Slug, settings.AdminEmail);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Dev seed skipped: {Reason}.")]
    private static partial void LogSkipped(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dev seed created club {Slug} with owner {Email}.")]
    private static partial void LogSeeded(ILogger logger, string slug, string email);
}
