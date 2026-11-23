using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Data;

public sealed class SeasonBackfillMigrationTests : IAsyncLifetime
{
    private const string MigrationBeforeSeasons = "20261121011149_SoftDeleteAndDeactivation";

    private readonly string connectionString = new SqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable(DatabaseFixture.ServerEnvironmentVariable)
            ?? @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True")
    {
        InitialCatalog = $"VolleyballTeam_Migration_{Guid.NewGuid():N}",
    }.ConnectionString;

    [Fact]
    public async Task Phase_0_teams_and_memberships_become_seasons_team_seasons_and_staff()
    {
        await using (var db = CreateContext(new TenantContext()))
        {
            await db.GetService<IMigrator>().MigrateAsync(MigrationBeforeSeasons);
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO [Clubs] ([Name], [Slug], [PrimaryColor], [SecondaryColor], [TimeZoneId], [CreatedAt], [UpdatedAt])
                VALUES (N'Legacy VBC', N'legacy', N'#1d4ed8', N'#f59e0b', N'UTC', SYSUTCDATETIME(), SYSUTCDATETIME());
                DECLARE @club int = SCOPE_IDENTITY();

                INSERT INTO [AspNetUsers] ([UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PhoneNumberConfirmed],
                    [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount], [FirstName], [LastName], [CreatedAt], [UpdatedAt])
                VALUES (N'coach@legacy.test', N'COACH@LEGACY.TEST', N'coach@legacy.test', N'COACH@LEGACY.TEST', 1, 0, 0, 1, 0, N'Old', N'Coach', SYSUTCDATETIME(), SYSUTCDATETIME()),
                       (N'parent@legacy.test', N'PARENT@LEGACY.TEST', N'parent@legacy.test', N'PARENT@LEGACY.TEST', 1, 0, 0, 1, 0, N'Old', N'Parent', SYSUTCDATETIME(), SYSUTCDATETIME());
                DECLARE @coach int = (SELECT [Id] FROM [AspNetUsers] WHERE [UserName] = N'coach@legacy.test');
                DECLARE @parent int = (SELECT [Id] FROM [AspNetUsers] WHERE [UserName] = N'parent@legacy.test');

                INSERT INTO [Teams] ([ClubId], [Name], [Season], [AgeGroup], [Level], [CreatedAt], [UpdatedAt])
                VALUES (@club, N'15U Black', N'2025-26', N'15U', N'Gold', SYSUTCDATETIME(), SYSUTCDATETIME()),
                       (@club, N'16U Black', N'2026-27', N'16U', N'Gold', SYSUTCDATETIME(), SYSUTCDATETIME());
                DECLARE @team int = (SELECT [Id] FROM [Teams] WHERE [Name] = N'16U Black');

                INSERT INTO [TeamMemberships] ([ClubId], [TeamId], [UserId], [Role], [CreatedAt], [UpdatedAt])
                VALUES (@club, @team, @coach, N'HeadCoach', SYSUTCDATETIME(), SYSUTCDATETIME()),
                       (@club, @team, @parent, N'Parent', SYSUTCDATETIME(), SYSUTCDATETIME());
                """);

            await db.Database.MigrateAsync();
        }

        await using var setup = CreateContext(new TenantContext());
        var club = await setup.Clubs.SingleAsync(c => c.Slug == "legacy");
        var tenant = new TenantContext();
        tenant.SetClub(club);
        await using var verify = CreateContext(tenant);

        var seasons = await verify.Seasons.OrderBy(s => s.Name).ToListAsync();
        Assert.Equal(["2025-26", "2026-27"], seasons.Select(s => s.Name));
        Assert.Equal([SeasonStatus.Archived, SeasonStatus.Active], seasons.Select(s => s.Status));

        var current = await verify.TeamSeasons.Include(t => t.Staff).SingleAsync(t => t.Season!.Name == "2026-27");
        Assert.Equal("16U Black", current.DisplayName);
        Assert.Equal("16U", current.AgeGroup);
        Assert.Equal("Gold", current.Level);

        var staff = Assert.Single(current.Staff);
        Assert.Equal(StaffRole.HeadCoach, staff.Role);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await using var db = CreateContext(new TenantContext());
        await db.Database.EnsureDeletedAsync();
    }

    private AppDbContext CreateContext(TenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options, tenant);
}
