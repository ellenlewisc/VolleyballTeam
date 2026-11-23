using System.Net;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Access;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Access;

public class TeamSeasonAccessRuleTests
{
    [Theory]
    [InlineData(ClubRole.Owner, SeasonStatus.Active, null, AccessLevel.Edit)]
    [InlineData(ClubRole.Admin, SeasonStatus.Upcoming, null, AccessLevel.Edit)]
    [InlineData(ClubRole.Admin, SeasonStatus.Archived, null, AccessLevel.ReadOnly)]
    [InlineData(ClubRole.Member, SeasonStatus.Active, StaffRole.HeadCoach, AccessLevel.Edit)]
    [InlineData(ClubRole.Member, SeasonStatus.Upcoming, StaffRole.AssistantCoach, AccessLevel.Edit)]
    [InlineData(ClubRole.Member, SeasonStatus.Active, StaffRole.TeamManager, AccessLevel.Edit)]
    [InlineData(ClubRole.Member, SeasonStatus.Archived, StaffRole.HeadCoach, AccessLevel.ReadOnly)]
    [InlineData(ClubRole.Member, SeasonStatus.Archived, StaffRole.AssistantCoach, AccessLevel.None)]
    [InlineData(ClubRole.Member, SeasonStatus.Archived, StaffRole.TeamManager, AccessLevel.None)]
    [InlineData(ClubRole.Member, SeasonStatus.Active, null, AccessLevel.None)]
    [InlineData(null, SeasonStatus.Active, StaffRole.HeadCoach, AccessLevel.None)]
    public void Decides_access_from_club_role_season_status_and_staff_role(
        ClubRole? clubRole, SeasonStatus status, StaffRole? staffRole, AccessLevel expected)
    {
        Assert.Equal(expected, TeamSeasonAccess.Decide(clubRole, status, staffRole).Level);
    }
}

[Collection(SharedDatabaseDefinition.Name)]
public class TeamSeasonHistoryAccessTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Former_head_coach_sees_their_past_season_read_only_but_not_the_teams_current_season()
    {
        var history = await SeedHistoryAsync();

        Assert.Equal(AccessLevel.ReadOnly, await LevelAsync(history.FormerHeadCoach, history.PastTeamSeasonId));
        Assert.Equal(AccessLevel.None, await LevelAsync(history.FormerHeadCoach, history.CurrentTeamSeasonId));
    }

    [Fact]
    public async Task Former_assistant_loses_access_to_the_past_season()
    {
        var history = await SeedHistoryAsync();

        Assert.Equal(AccessLevel.None, await LevelAsync(history.FormerAssistant, history.PastTeamSeasonId));
    }

    [Fact]
    public async Task New_head_coach_edits_the_current_season_but_not_seasons_they_did_not_coach()
    {
        var history = await SeedHistoryAsync();

        Assert.Equal(AccessLevel.Edit, await LevelAsync(history.NewHeadCoach, history.CurrentTeamSeasonId));
        Assert.Equal(AccessLevel.None, await LevelAsync(history.NewHeadCoach, history.PastTeamSeasonId));
    }

    [Fact]
    public async Task Admin_reads_the_past_and_edits_the_present()
    {
        var history = await SeedHistoryAsync();

        Assert.Equal(AccessLevel.ReadOnly, await LevelAsync(history.Admin, history.PastTeamSeasonId));
        Assert.Equal(AccessLevel.Edit, await LevelAsync(history.Admin, history.CurrentTeamSeasonId));
    }

    [Fact]
    public async Task Deactivated_former_head_coach_loses_their_history()
    {
        var history = await SeedHistoryAsync();
        await using (var db = fixture.CreateContextFor(fixture.Alpha.Club))
        {
            var membership = db.ClubMemberships.Single(m => m.UserId == history.FormerHeadCoach);
            membership.DeactivatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        Assert.Equal(AccessLevel.None, await LevelAsync(history.FormerHeadCoach, history.PastTeamSeasonId));
    }

    [Fact]
    public async Task Dashboard_lists_a_former_head_coachs_past_season_only()
    {
        var history = await SeedHistoryAsync();
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var teams = new TeamService(db, new TeamSeasonAccessService(db));

        var cards = await teams.GetTeamSeasonsForUserAsync(history.FormerHeadCoach);

        var card = Assert.Single(cards);
        Assert.Equal(history.PastTeamSeasonId, card.Id);
        Assert.Equal(AccessLevel.ReadOnly, card.Access);
    }

    [Fact]
    public async Task Former_head_coach_is_denied_the_old_teams_current_page_and_cannot_edit_the_past()
    {
        var history = await SeedHistoryAsync();
        using var client = fixture.Web.CreateClientFor(history.FormerHeadCoach);

        var past = await client.GetAsync($"/alpha/teams/{history.PastTeamSeasonId}");
        var pastEdit = await client.GetAsync($"/alpha/teams/{history.PastTeamSeasonId}/edit");
        var current = await client.GetAsync($"/alpha/teams/{history.CurrentTeamSeasonId}");

        Assert.Equal(HttpStatusCode.OK, past.StatusCode);
        Assert.Contains("Read-only", await past.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Redirect, pastEdit.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, current.StatusCode);
        Assert.Contains("/account/access-denied", current.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Season_admin_pages_are_for_club_admins_only()
    {
        var history = await SeedHistoryAsync();
        using var coach = fixture.Web.CreateClientFor(history.NewHeadCoach);
        using var admin = fixture.Web.CreateClientFor(history.Admin);

        Assert.Equal(HttpStatusCode.Redirect, (await coach.GetAsync("/alpha/admin/seasons")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/alpha/admin/seasons")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/alpha/admin/seasons/{history.CurrentSeasonId}/rollover")).StatusCode);
    }

    private async Task<AccessLevel> LevelAsync(int userId, int teamSeasonId)
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        return (await new TeamSeasonAccessService(db).GetAsync(userId, teamSeasonId)).Level;
    }

    private async Task<History> SeedHistoryAsync()
    {
        var formerHeadCoach = await fixture.AddMemberAsync(fixture.Alpha.Club);
        var formerAssistant = await fixture.AddMemberAsync(fixture.Alpha.Club);
        var newHeadCoach = await fixture.AddMemberAsync(fixture.Alpha.Club);
        var admin = await fixture.AddMemberAsync(fixture.Alpha.Club, clubRole: ClubRole.Admin);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var team = new Team { Name = $"Red {suffix}" };
        var past = new TeamSeason
        {
            Team = team,
            Season = new Season { Name = $"P{suffix}", Status = SeasonStatus.Archived },
            DisplayName = "16U Red",
            Staff =
            [
                new TeamSeasonStaff { UserId = formerHeadCoach, Role = StaffRole.HeadCoach },
                new TeamSeasonStaff { UserId = formerAssistant, Role = StaffRole.AssistantCoach },
            ],
        };
        var current = new TeamSeason
        {
            Team = team,
            Season = new Season { Name = $"C{suffix}", Status = SeasonStatus.Active },
            DisplayName = "17U Red",
            RolledOverFrom = past,
            Staff = [new TeamSeasonStaff { UserId = newHeadCoach, Role = StaffRole.HeadCoach }],
        };

        db.TeamSeasons.AddRange(past, current);
        await db.SaveChangesAsync();

        return new History(formerHeadCoach, formerAssistant, newHeadCoach, admin, past.Id, current.Id, current.SeasonId);
    }

    private sealed record History(
        int FormerHeadCoach, int FormerAssistant, int NewHeadCoach, int Admin, int PastTeamSeasonId, int CurrentTeamSeasonId, int CurrentSeasonId);
}
