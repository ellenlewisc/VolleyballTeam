using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Access;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Tenancy;

[Collection(SharedDatabaseDefinition.Name)]
public class TenantIsolationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Queries_only_return_rows_from_the_current_club()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);

        var teams = await db.Teams.ToListAsync();
        var clubMemberships = await db.ClubMemberships.ToListAsync();
        var seasons = await db.Seasons.ToListAsync();
        var teamSeasons = await db.TeamSeasons.ToListAsync();
        var staff = await db.TeamSeasonStaff.ToListAsync();

        Assert.NotEmpty(teams);
        Assert.All(teams, t => Assert.Equal(fixture.Alpha.Club.Id, t.ClubId));
        Assert.All(clubMemberships, m => Assert.Equal(fixture.Alpha.Club.Id, m.ClubId));
        Assert.All(seasons, s => Assert.Equal(fixture.Alpha.Club.Id, s.ClubId));
        Assert.All(teamSeasons, t => Assert.Equal(fixture.Alpha.Club.Id, t.ClubId));
        Assert.All(staff, s => Assert.Equal(fixture.Alpha.Club.Id, s.ClubId));
    }

    [Fact]
    public async Task Another_clubs_row_cannot_be_found_by_id()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);

        var team = await db.Teams.SingleOrDefaultAsync(t => t.Id == fixture.Bravo.TeamId);

        Assert.Null(team);
    }

    [Fact]
    public async Task Queries_without_a_club_return_nothing()
    {
        await using var db = fixture.CreateContext(new TenantContext());

        Assert.Empty(await db.Teams.ToListAsync());
        Assert.Empty(await db.ClubMemberships.ToListAsync());
    }

    [Fact]
    public async Task New_rows_are_stamped_with_the_current_club()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var team = new Team { Name = $"Stamped {Guid.NewGuid():N}" };

        db.Teams.Add(team);
        await db.SaveChangesAsync();

        Assert.Equal(fixture.Alpha.Club.Id, team.ClubId);
        Assert.NotEqual(default, team.CreatedAt);
    }

    [Fact]
    public async Task Adding_a_row_for_another_club_throws()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        db.Teams.Add(new Team { ClubId = fixture.Bravo.Club.Id, Name = "Sneaky" });

        await Assert.ThrowsAsync<TenantViolationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Saving_without_a_club_throws()
    {
        await using var db = fixture.CreateContext(new TenantContext());
        db.Teams.Add(new Team { Name = "Orphan" });

        await Assert.ThrowsAsync<TenantViolationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Modifying_another_clubs_row_throws()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var bravoTeam = await db.Teams.IgnoreQueryFilters().SingleAsync(t => t.Id == fixture.Bravo.TeamId);

        bravoTeam.Name = "Renamed by Alpha";

        await Assert.ThrowsAsync<TenantViolationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Deleting_another_clubs_row_throws()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var bravoTeam = await db.Teams.IgnoreQueryFilters().SingleAsync(t => t.Id == fixture.Bravo.TeamId);

        db.Teams.Remove(bravoTeam);

        await Assert.ThrowsAsync<TenantViolationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Moving_a_row_into_the_current_club_throws()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var bravoMembership = await db.ClubMemberships.IgnoreQueryFilters()
            .FirstAsync(m => m.ClubId == fixture.Bravo.Club.Id);

        bravoMembership.ClubId = fixture.Alpha.Club.Id;

        await Assert.ThrowsAsync<TenantViolationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Team_club_cannot_be_reassigned()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var team = await db.Teams.SingleAsync(t => t.Id == fixture.Alpha.TeamId);

        team.ClubId = fixture.Bravo.Club.Id;

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_rejects_staff_that_points_at_another_clubs_team_season()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        db.TeamSeasonStaff.Add(new TeamSeasonStaff
        {
            UserId = fixture.Alpha.CoachUserId,
            TeamSeasonId = fixture.Bravo.TeamSeasonId,
            Role = StaffRole.AssistantCoach,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_rejects_a_team_season_that_mixes_clubs()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        db.TeamSeasons.Add(new TeamSeason
        {
            TeamId = fixture.Alpha.TeamId,
            SeasonId = fixture.Bravo.SeasonId,
            DisplayName = $"Mixed {Guid.NewGuid():N}",
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_rejects_an_event_on_another_clubs_team_season()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        db.TeamEvents.Add(new TeamEvent
        {
            TeamSeasonId = fixture.Bravo.TeamSeasonId,
            Type = EventType.Practice,
            StartUtc = new DateTime(2030, 1, 1, 18, 0, 0),
            EndUtc = new DateTime(2030, 1, 1, 20, 0, 0),
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Team_service_returns_nothing_for_a_user_outside_the_club()
    {
        await using var db = fixture.CreateContextFor(fixture.Bravo.Club);
        var teams = new TeamService(db, new TeamSeasonAccessService(db));

        var result = await teams.GetTeamSeasonsForUserAsync(fixture.Alpha.CoachUserId);

        Assert.Empty(result);
    }
}
