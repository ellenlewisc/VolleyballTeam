using System.Net;
using System.Text.RegularExpressions;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Web;

[Collection(SharedDatabaseDefinition.Name)]
public partial class ScheduleEndpointTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Head_coach_adds_a_game_and_sees_it_on_the_schedule()
    {
        using var client = fixture.Web.CreateClientFor(fixture.Alpha.CoachUserId);
        var schedulePath = $"/alpha/teams/{fixture.Alpha.TeamSeasonId}/schedule";
        var opponent = $"Rivals {Guid.NewGuid():N}"[..20];

        var form = await (await client.GetAsync($"{schedulePath}/new")).Content.ReadAsStringAsync();
        var saved = await client.PostAsync($"{schedulePath}/new", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Type"] = "Game",
            ["StartsAt"] = "2035-05-01T18:00",
            ["EndsAt"] = "2035-05-01T20:00",
            ["Opponent"] = opponent,
            ["Location"] = "Main gym",
            ["__RequestVerificationToken"] = Token(form),
        }));
        var html = await (await client.GetAsync(schedulePath)).Content.ReadAsStringAsync();

        Assert.Contains("Starts<span class=\"required-mark\"", form, StringComparison.Ordinal);
        Assert.DoesNotContain("Title<span class=\"required-mark\"", form, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.Contains("Add event", html, StringComparison.Ordinal);
        Assert.Contains($"Game vs {opponent}", html, StringComparison.Ordinal);
        Assert.Contains("Tue, May 1 · 6:00 PM – 8:00 PM", html.Replace("&#xB7;", "·", StringComparison.Ordinal).Replace("&#x2013;", "–", StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Club_member_without_the_team_is_denied_the_schedule()
    {
        var outsider = await fixture.AddMemberAsync(fixture.Alpha.Club);
        using var client = fixture.Web.CreateClientFor(outsider);

        var page = await client.GetAsync($"/alpha/teams/{fixture.Alpha.TeamSeasonId}/schedule");

        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
        Assert.Contains("/account/access-denied", page.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Only_admins_change_club_settings()
    {
        var admin = await fixture.AddMemberAsync(fixture.Alpha.Club, clubRole: ClubRole.Admin);
        using var coach = fixture.Web.CreateClientFor(fixture.Alpha.CoachUserId);
        using var adminClient = fixture.Web.CreateClientFor(admin);

        var page = await (await adminClient.GetAsync("/alpha/admin/settings")).Content.ReadAsStringAsync();
        var rejected = await PostSettingsAsync(adminClient, page, "Mars/Olympus");
        var saved = await PostSettingsAsync(adminClient, page, "UTC");

        Assert.Equal(HttpStatusCode.Redirect, (await coach.GetAsync("/alpha/admin/settings")).StatusCode);
        Assert.Contains("Choose a time zone from the list.", await rejected.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
    }

    private Task<HttpResponseMessage> PostSettingsAsync(HttpClient client, string pageHtml, string timeZoneId) =>
        client.PostAsync("/alpha/admin/settings", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = fixture.Alpha.Club.Name,
            ["TimeZoneId"] = timeZoneId,
            ["__RequestVerificationToken"] = Token(pageHtml),
        }));

    private static string Token(string html) => TokenPattern().Match(html).Groups[1].Value;

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();
}
