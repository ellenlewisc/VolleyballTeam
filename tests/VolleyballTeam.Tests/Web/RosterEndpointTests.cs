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
    public async Task Player_directory_is_for_club_admins_only()
    {
        using var coach = fixture.Web.CreateClientFor(fixture.Alpha.CoachUserId);

        Assert.Equal(HttpStatusCode.Redirect, (await coach.GetAsync("/alpha/admin/players")).StatusCode);
    }

    [Fact]
    public async Task Player_directory_data_is_served_over_post_with_named_sorting()
    {
        var admin = await fixture.AddMemberAsync(fixture.Alpha.Club, clubRole: ClubRole.Admin);
        using var client = fixture.Web.CreateClientFor(admin);

        var page = await client.GetAsync("/alpha/admin/players");
        var token = TokenPattern().Match(await page.Content.ReadAsStringAsync()).Groups[1].Value;

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["draw"] = "3",
            ["start"] = "0",
            ["length"] = "10",
            ["search[value]"] = "",
            ["order[0][name]"] = "gradYear",
            ["order[0][dir]"] = "desc",
            ["__RequestVerificationToken"] = token,
        });
        var response = await client.PostAsync("/alpha/admin/players/data", form);
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"draw\":3", json, StringComparison.Ordinal);
        Assert.Contains("\"recordsTotal\"", json, StringComparison.Ordinal);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();
}
