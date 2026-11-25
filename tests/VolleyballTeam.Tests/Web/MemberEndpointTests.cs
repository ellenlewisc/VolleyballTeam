using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Tests.Infrastructure;

namespace VolleyballTeam.Tests.Web;

[Collection(SharedDatabaseDefinition.Name)]
public partial class MemberEndpointTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Users_page_is_for_club_admins_only()
    {
        var admin = await fixture.AddMemberAsync(fixture.Alpha.Club, clubRole: ClubRole.Admin);
        using var coach = fixture.Web.CreateClientFor(fixture.Alpha.CoachUserId);
        using var adminClient = fixture.Web.CreateClientFor(admin);

        Assert.Equal(HttpStatusCode.Redirect, (await coach.GetAsync("/alpha/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync("/alpha/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync("/alpha/admin/users/invite")).StatusCode);
    }

    [Fact]
    public async Task Only_admins_can_invite_staff_to_a_team()
    {
        var admin = await fixture.AddMemberAsync(fixture.Alpha.Club, clubRole: ClubRole.Admin);
        using var headCoach = fixture.Web.CreateClientFor(fixture.Alpha.CoachUserId);
        using var adminClient = fixture.Web.CreateClientFor(admin);
        var invitePath = $"/alpha/teams/{fixture.Alpha.TeamSeasonId}/invite";

        Assert.Equal(HttpStatusCode.Redirect, (await headCoach.GetAsync(invitePath)).StatusCode);
        Assert.DoesNotContain("Invite staff", await (await headCoach.GetAsync($"/alpha/teams/{fixture.Alpha.TeamSeasonId}")).Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync(invitePath)).StatusCode);
    }

    [Fact]
    public async Task Invitation_link_shows_the_sign_up_form_to_a_new_person()
    {
        string token;
        using (var scope = fixture.Web.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<TenantContext>().SetClub(fixture.Alpha.Club);
            var invite = await scope.ServiceProvider.GetRequiredService<InvitationService>()
                .CreateAsync(new InviteRequest($"new-{Guid.NewGuid():N}@alpha.test", "Jo", "Kim", ClubRole.Member, null, null));
            token = invite.Token!;
        }

        using var client = fixture.Web.CreateClientFor(userId: null);
        var page = await client.GetAsync($"/alpha/join/{token}");
        var bogus = await client.GetAsync("/alpha/join/not-a-real-token");

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Create account and join", await page.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Contains("expired or isn", await bogus.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Forgot_password_gives_the_same_answer_for_unknown_emails()
    {
        using var client = fixture.Web.CreateClientFor(userId: null);
        var form = await client.GetAsync("/account/forgot-password");
        var token = TokenPattern().Match(await form.Content.ReadAsStringAsync()).Groups[1].Value;

        using var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = $"nobody-{Guid.NewGuid():N}@nowhere.test",
            ["__RequestVerificationToken"] = token,
        });
        var response = await client.PostAsync("/account/forgot-password", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("If an account exists", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();
}
