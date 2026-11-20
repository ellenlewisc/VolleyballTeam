using System.Net;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Web;

[Collection(SharedDatabaseDefinition.Name)]
public class ClubEndpointTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Anonymous_user_is_sent_to_login()
    {
        using var client = fixture.Web.CreateClientFor(userId: null);

        var response = await client.GetAsync("/alpha");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Member_sees_their_club_dashboard()
    {
        using var client = fixture.Web.CreateClientFor(fixture.Alpha.CoachUserId);

        var response = await client.GetAsync("/alpha");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(fixture.Alpha.Club.Name, html, StringComparison.Ordinal);
        Assert.Contains(fixture.Alpha.TeamName, html, StringComparison.Ordinal);
        Assert.DoesNotContain(fixture.Bravo.TeamName, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Member_of_one_club_is_denied_another_club()
    {
        using var client = fixture.Web.CreateClientFor(fixture.Alpha.CoachUserId);

        var response = await client.GetAsync("/bravo");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/access-denied", response.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_club_returns_not_found()
    {
        using var client = fixture.Web.CreateClientFor(fixture.Alpha.CoachUserId);

        var response = await client.GetAsync("/no-such-club");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Home_redirects_a_single_club_member_to_their_club()
    {
        using var client = fixture.Web.CreateClientFor(fixture.Alpha.CoachUserId);

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/alpha", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Login_page_renders_for_anonymous_users()
    {
        using var client = fixture.Web.CreateClientFor(userId: null);

        var response = await client.GetAsync("/account/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
