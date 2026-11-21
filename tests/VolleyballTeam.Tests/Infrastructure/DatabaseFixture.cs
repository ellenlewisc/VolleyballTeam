using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Auditing;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Data.Interceptors;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Tests.Infrastructure;

public record SeededClub(Club Club, int CoachUserId, int TeamId, string TeamName);

public sealed class DatabaseFixture : IAsyncLifetime
{
    public const string ServerEnvironmentVariable = "VBT_TEST_SQLSERVER";
    private const string LocalDbServer = @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";

    public string ConnectionString { get; } = new SqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable(ServerEnvironmentVariable) ?? LocalDbServer)
    {
        InitialCatalog = $"VolleyballTeam_Tests_{Guid.NewGuid():N}",
        MultipleActiveResultSets = true,
    }.ConnectionString;

    public SeededClub Alpha { get; private set; } = null!;
    public SeededClub Bravo { get; private set; } = null!;
    public VolleyballWebFactory Web { get; private set; } = null!;

    public AppDbContext CreateContext(TenantContext tenant, int? actingUserId = null)
    {
        var currentUser = new CurrentUser();
        if (actingUserId is int userId)
        {
            currentUser.Set(userId);
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(
                new TenantWriteInterceptor(tenant),
                new SoftDeleteInterceptor(TimeProvider.System, currentUser),
                new AuditInterceptor(TimeProvider.System, currentUser))
            .Options;

        return new AppDbContext(options, tenant);
    }

    public AppDbContext CreateContextFor(Club club, int? actingUserId = null)
    {
        var tenant = new TenantContext();
        tenant.SetClub(club);
        return CreateContext(tenant, actingUserId);
    }

    public async Task InitializeAsync()
    {
        await using (var db = CreateContext(new TenantContext()))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        Alpha = await SeedClubAsync("Alpha VBC", "alpha");
        Bravo = await SeedClubAsync("Bravo VBC", "bravo");
        Web = new VolleyballWebFactory(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        await Web.DisposeAsync();
        await using var db = CreateContext(new TenantContext());
        await db.Database.EnsureDeletedAsync();
    }

    public async Task<int> AddMemberAsync(Club club, int? teamId = null)
    {
        await using var db = CreateContextFor(club);
        var email = $"member-{Guid.NewGuid():N}@{club.Slug}.test";
        var user = new AppUser
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            FirstName = "Member",
            LastName = club.Name,
        };

        db.Users.Add(user);
        db.ClubMemberships.Add(new ClubMembership { User = user, Role = ClubRole.Member });
        if (teamId is int id)
        {
            db.TeamMemberships.Add(new TeamMembership { User = user, TeamId = id, Role = TeamRole.Parent });
        }

        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<SeededClub> SeedClubAsync(string name, string slug)
    {
        var club = new Club { Name = name, Slug = slug };
        await using (var db = CreateContext(new TenantContext()))
        {
            db.Clubs.Add(club);
            await db.SaveChangesAsync();
        }

        await using var clubDb = CreateContextFor(club);
        var email = $"coach@{slug}.test";
        var coach = new AppUser
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            FirstName = "Coach",
            LastName = name,
        };
        var team = new Team { Name = $"{name} 16U", Season = "2026-27" };

        clubDb.Users.Add(coach);
        clubDb.Teams.Add(team);
        clubDb.ClubMemberships.Add(new ClubMembership { User = coach, Role = ClubRole.Member });
        clubDb.TeamMemberships.Add(new TeamMembership { User = coach, Team = team, Role = TeamRole.HeadCoach });
        await clubDb.SaveChangesAsync();

        return new SeededClub(club, coach.Id, team.Id, team.Name);
    }
}

[CollectionDefinition(Name)]
public class SharedDatabaseDefinition : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
