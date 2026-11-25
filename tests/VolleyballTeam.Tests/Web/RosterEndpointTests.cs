using System.Net;
using System.Text.RegularExpressions;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Web;

[Collection(SharedDatabaseDefinition.Name)]
public partial class RosterEndpointTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Head_coach_sees_and_can_manage_their_roster()
    {
        using var client = fixture.Web.CreateClientFor(fixture.Alpha.CoachUserId);

        var page = await client.GetAsync($"/alpha/teams/{fixture.Alpha.TeamSeasonId}/roster");
        var html = await page.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Add player", html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/alpha/teams/{fixture.Alpha.TeamSeasonId}/roster/export")).StatusCode);
    }

    [Fact]
    public async Task Club_member_without_the_team_is_denied_the_roster()
    {
        var outsider = await fixture.AddMemberAsync(fixture.Alpha.Club);
        using var client = fixture.Web.CreateClientFor(outsider);

        var page = await client.GetAsync($"/alpha/teams/{fixture.Alpha.TeamSeasonId}/roster");

        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
        Assert.Contains("/account/access-denied", page.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Coach_sees_players_for_their_own_team_only()
    {
        var otherTeam = await NewTeamSeasonAsync();
        using var coach = fixture.Web.CreateClientFor(fixture.Alpha.CoachUserId);

        var page = await coach.GetAsync("/alpha/players");
        var html = await page.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.DoesNotContain("All teams", html, StringComparison.Ordinal);
        Assert.Contains($"value=\"{fixture.Alpha.TeamSeasonId}\" selected=\"selected\"", html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await PostDataAsync(coach, html, fixture.Alpha.TeamSeasonId)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await PostDataAsync(coach, html, otherTeam)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await coach.GetAsync($"/alpha/players/{otherTeam}")).StatusCode);
    }

    [Fact]
    public async Task Admin_sees_all_players_with_named_sorting_over_post()
    {
        var admin = await fixture.AddMemberAsync(fixture.Alpha.Club, clubRole: ClubRole.Admin);
        using var client = fixture.Web.CreateClientFor(admin);

        var page = await client.GetAsync("/alpha/players");
        var html = await page.Content.ReadAsStringAsync();
        var response = await PostDataAsync(client, html, teamSeasonId: null);
        var json = await response.Content.ReadAsStringAsync();

        Assert.Contains("All teams", html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"draw\":3", json, StringComparison.Ordinal);
        Assert.Contains("\"recordsTotal\"", json, StringComparison.Ordinal);
    }

    private static Task<HttpResponseMessage> PostDataAsync(HttpClient client, string pageHtml, int? teamSeasonId)
    {
        var fields = new Dictionary<string, string>
        {
            ["draw"] = "3",
            ["start"] = "0",
            ["length"] = "10",
            ["search[value]"] = "",
            ["order[0][name]"] = "gradYear",
            ["order[0][dir]"] = "desc",
            ["__RequestVerificationToken"] = TokenPattern().Match(pageHtml).Groups[1].Value,
        };
        if (teamSeasonId is int id)
        {
            fields["teamSeasonId"] = id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return client.PostAsync("/alpha/players/data", new FormUrlEncodedContent(fields));
    }

    private async Task<int> NewTeamSeasonAsync()
    {
        await using var db = fixture.CreateContextFor(fixture.Alpha.Club);
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var teamSeason = new VolleyballTeam.Domain.Entities.TeamSeason
        {
            Team = new VolleyballTeam.Domain.Entities.Team { Name = $"Other {suffix}" },
            SeasonId = fixture.Alpha.SeasonId,
            DisplayName = $"Other {suffix}",
        };
        db.TeamSeasons.Add(teamSeason);
        await db.SaveChangesAsync();
        return teamSeason.Id;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();
}
